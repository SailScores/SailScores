using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using SailScores.Core.Services;


namespace SailScores.Web.Services;

public interface IStripeService
{
    Task<Session> CreateCheckoutSessionAsync(
        string plan,
        string userEmail,
        string domain);

    Task HandleStripeWebhookAsync(string json, string stripeSignature, ILogger logger);

    Task<(string ClubId, string ClubInitials)> GetFirstClubForUserEmailAsync(string userEmail);

    Task<bool> UserHasMultipleClubsAsync(string userEmail);
}

public class StripeService : IStripeService
{
    private readonly IConfiguration _configuration;
    private readonly SailScores.Web.Services.Interfaces.IClubService _clubService;
    private readonly IUserService _userService;

    public StripeService(IConfiguration configuration, SailScores.Web.Services.Interfaces.IClubService clubService, IUserService userService)
    {
        _configuration = configuration;
        _clubService = clubService;
        _userService = userService;
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    public async Task<Session> CreateCheckoutSessionAsync(
        string plan,
        string userEmail,
        string domain)
    {
        var clubInfo = await GetFirstClubForUserEmailAsync(userEmail);

        var clubId = clubInfo.ClubId;
        var clubInitials = clubInfo.ClubInitials;

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>(),
            Mode = "subscription",
            SuccessUrl = domain + "/Supporter/Success?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = domain + "/Supporter/Cancel",
            Metadata = new Dictionary<string, string>
            {
                { "clubId", clubId ?? string.Empty },
                { "clubInitials", clubInitials ?? string.Empty },
                { "userEmail", userEmail ?? string.Empty }
            }
        };

        if (!string.IsNullOrEmpty(userEmail))
        {
            options.CustomerEmail = userEmail;
        }

        // Handle plan types:
        // - "monthly" -> recurring monthly price id
        // - "yearly"  -> recurring annual price id
        // - "custom" -> one-time payment
        if (!string.IsNullOrWhiteSpace(plan) &&
            plan.StartsWith("custom", StringComparison.OrdinalIgnoreCase))
        {
            options.Mode = "payment";
            var priceId = _configuration["Stripe:UserSelectsPriceId"];
            
            if (string.IsNullOrWhiteSpace(priceId))
            {
                throw new InvalidOperationException(
                    "Stripe configuration for custom pricing is missing. " +
                    "Please ensure 'Stripe:UserSelectsPriceId' is configured in application settings.");
            }
            
            options.LineItems.Add(new SessionLineItemOptions
            {
                Price = priceId,
                Quantity = 1,
            });
        }
        else
        {
            // Recurring subscription branch (monthly / yearly)
            var priceId = plan == "yearly"
                ? _configuration["Stripe:YearlyPriceId"]
                : _configuration["Stripe:MonthlyPriceId"];

            if (string.IsNullOrWhiteSpace(priceId))
            {
                var configKey = plan == "yearly" ? "Stripe:YearlyPriceId" : "Stripe:MonthlyPriceId";
                throw new InvalidOperationException(
                    $"Stripe configuration for {plan} pricing is missing. " +
                    $"Please ensure '{configKey}' is configured in application settings.");
            }

            options.Mode = "subscription";
            options.LineItems.Add(new SessionLineItemOptions
            {
                Price = priceId,
                Quantity = 1,
            });
        }

        // Add custom fields for visibility in Stripe dashboard
        if (!string.IsNullOrWhiteSpace(clubInitials))
        {
            options.CustomFields = new List<SessionCustomFieldOptions>
            {
                new SessionCustomFieldOptions
                {
                    Key = "club_initials",
                    Label = new SessionCustomFieldLabelOptions
                    {
                        Type = "custom",
                        Custom = "Club Initials"
                    },
                    Type = "text",
                    Text = new SessionCustomFieldTextOptions
                    {
                        DefaultValue = clubInitials
                    }
                }
            };
        }

        var service = new SessionService();
        return await service.CreateAsync(options);
    }

    public async Task HandleStripeWebhookAsync(string json, string stripeSignature, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentNullException(nameof(json), "Webhook JSON body cannot be null or empty");
        }

        if (string.IsNullOrWhiteSpace(stripeSignature))
        {
            throw new ArgumentNullException(nameof(stripeSignature), "Stripe signature cannot be null or empty");
        }

        var webhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET") ?? _configuration["Stripe:WebhookSecret"];
        
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            logger.LogError("Stripe webhook secret is not configured. Set STRIPE_WEBHOOK_SECRET environment variable or Stripe:WebhookSecret in configuration.");
            throw new InvalidOperationException("Stripe webhook secret is not configured");
        }

        Stripe.Event stripeEvent;
        try
        {
            // Allow different API versions to avoid version mismatch errors
            stripeEvent = Stripe.EventUtility.ConstructEvent(
                json, 
                stripeSignature, 
                webhookSecret, 
                throwOnApiVersionMismatch: false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Stripe webhook signature verification failed.");
            throw;
        }

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                await HandleCheckoutSessionCompletedAsync(stripeEvent, logger);
                break;
            case "invoice.paid":
                await HandleInvoicePaidAsync(stripeEvent, logger);
                break;
            case "invoice.payment_failed":
                await HandleInvoicePaymentFailedAsync(stripeEvent, logger);
                break;
            case "customer.subscription.updated":
                HandleSubscriptionUpdated(stripeEvent, logger);
                break;
            case "customer.subscription.deleted":
                HandleSubscriptionDeleted(stripeEvent, logger);
                break;
            default:
                logger.LogInformation($"Unhandled Stripe event type: {stripeEvent.Type}");
                break;
        }
    }

    private async Task HandleCheckoutSessionCompletedAsync(Stripe.Event stripeEvent, ILogger logger)
    {
        var session = stripeEvent.Data.Object as Session;
        var sessionClubId = session?.Metadata?.GetValueOrDefault("clubId");
        var sessionClubInitials = session?.Metadata?.GetValueOrDefault("clubInitials");
        var email = session?.CustomerEmail;

        var customFields = session?.CustomFields;

        // if custom field for club initials exists and is a defined club, use that
        if (customFields != null)
        {
            var clubInitialsField = customFields
                .FirstOrDefault(f => f.Key == "club_initials" && f.Type == "text");
            if (clubInitialsField != null && 
                !string.IsNullOrWhiteSpace(clubInitialsField.Text?.Value))
            {
                var providedInitials = clubInitialsField.Text.Value;
                try
                {
                    var clubId = await _clubService.GetClubId(providedInitials);
                    if (clubId != null)
                    {
                        sessionClubInitials = providedInitials;
                        sessionClubId = clubId.ToString();
                        logger.LogInformation($"Using club initials from custom field: {providedInitials} maps to club ID {sessionClubId}");
                    }
                    else
                    {
                        logger.LogWarning($"Club initials from custom field '{providedInitials}' did not map to a valid club");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Error looking up club by initials '{providedInitials}' from custom field");
                }
            }
        }

        logger.LogInformation($"checkout.session.completed: clubId={sessionClubId}, clubInitials={sessionClubInitials}, email={email}");
        
        // Determine subscription type from the session
        string subscriptionType = await DetermineSubscriptionTypeFromSessionAsync(session, logger);
        
        await EnableAdvancedFeaturesAsync(email, sessionClubId, sessionClubInitials, subscriptionType, logger, "checkout.session.completed");
    }

    private async Task HandleInvoicePaidAsync(Stripe.Event stripeEvent, ILogger logger)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        var clubId = invoice?.Metadata?.GetValueOrDefault("clubId");
        var clubInitials = invoice?.Metadata?.GetValueOrDefault("clubInitials");
        var email = invoice?.CustomerEmail;
        
        logger.LogInformation($"invoice.paid: clubId={clubId}, clubInitials={clubInitials}, email={email}");
        
        // Determine subscription type from the invoice
        string subscriptionType = await DetermineSubscriptionTypeFromInvoiceAsync(invoice, logger);
        
        await EnableAdvancedFeaturesAsync(email, clubId, clubInitials, subscriptionType, logger, "invoice.paid");
    }

    private async Task HandleInvoicePaymentFailedAsync(Stripe.Event stripeEvent, ILogger logger)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        var clubId = invoice?.Metadata?.GetValueOrDefault("clubId");
        var clubInitials = invoice?.Metadata?.GetValueOrDefault("clubInitials");
        var email = invoice?.CustomerEmail;
        
        logger.LogInformation($"invoice.payment_failed: clubId={clubId}, clubInitials={clubInitials}, email={email}");
        
        await DisableAdvancedFeaturesAsync(email, clubId, clubInitials, logger, "invoice.payment_failed");
    }

    private void HandleSubscriptionUpdated(Stripe.Event stripeEvent, ILogger logger)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        var clubId = subscription?.Metadata?.GetValueOrDefault("clubId");
        var clubInitials = subscription?.Metadata?.GetValueOrDefault("clubInitials");
        
        logger.LogInformation($"Subscription updated: {{Id={subscription?.Id}}}, clubId={clubId}, clubInitials={clubInitials}");
    }

    private void HandleSubscriptionDeleted(Stripe.Event stripeEvent, ILogger logger)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        var clubId = subscription?.Metadata?.GetValueOrDefault("clubId");
        var clubInitials = subscription?.Metadata?.GetValueOrDefault("clubInitials");
        
        logger.LogInformation($"Subscription deleted: {{Id={subscription?.Id}}}, clubId={clubId}, clubInitials={clubInitials}");
    }

    private async Task<string> DetermineSubscriptionTypeFromSessionAsync(Session session, ILogger logger)
    {
        if (session?.SubscriptionId == null)
        {
            logger.LogWarning("Session does not have a subscription ID, cannot determine subscription type");
            return null;
        }

        try
        {
            var subscriptionService = new SubscriptionService();
            var subscription = await subscriptionService.GetAsync(session.SubscriptionId);
            return DetermineSubscriptionTypeFromSubscription(subscription, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error retrieving subscription {session.SubscriptionId} to determine type");
            return null;
        }
    }

    private async Task<string> DetermineSubscriptionTypeFromInvoiceAsync(Invoice invoice, ILogger logger)
    {
        // Get subscription object from invoice lines
        var subscription = invoice?.Lines?.Data?.FirstOrDefault()?.Subscription;
        if (subscription == null)
        {
            logger.LogWarning("Invoice does not have a subscription, cannot determine subscription type");
            return null;
        }

        return DetermineSubscriptionTypeFromSubscription(subscription, logger);
    }

    private string DetermineSubscriptionTypeFromSubscription(Subscription subscription, ILogger logger)
    {
        if (subscription?.Items?.Data == null || !subscription.Items.Data.Any())
        {
            logger.LogWarning("Subscription has no items, cannot determine type");
            return null;
        }

        var priceId = subscription.Items.Data.First().Price?.Id;
        var monthlyPriceId = _configuration["Stripe:MonthlyPriceId"];
        var yearlyPriceId = _configuration["Stripe:YearlyPriceId"];

        if (priceId == monthlyPriceId)
        {
            logger.LogInformation($"Subscription type determined as Monthly (price ID: {priceId})");
            return "Monthly";
        }
        else if (priceId == yearlyPriceId)
        {
            logger.LogInformation($"Subscription type determined as Annual (price ID: {priceId})");
            return "Annual";
        }
        else
        {
            // Fallback: check the interval
            var interval = subscription.Items.Data.First().Price?.Recurring?.Interval;
            if (interval == "month")
            {
                logger.LogInformation($"Subscription type determined as Monthly from interval");
                return "Monthly";
            }
            else if (interval == "year")
            {
                logger.LogInformation($"Subscription type determined as Annual from interval");
                return "Annual";
            }
            
            logger.LogWarning($"Could not determine subscription type from price ID {priceId} or interval {interval}");
            return null;
        }
    }

    private async Task EnableAdvancedFeaturesAsync(
        string email, 
        string clubIdFromMetadata, 
        string clubInitialsFromMetadata,
        string subscriptionType,
        ILogger logger, 
        string eventSource)
    {
        var clubIdsToEnable = new HashSet<Guid>();

        // If specific club ID is provided in metadata, add it
        if (!string.IsNullOrWhiteSpace(clubIdFromMetadata) && Guid.TryParse(clubIdFromMetadata, out var specificClubId))
        {
            clubIdsToEnable.Add(specificClubId);
            logger.LogInformation($"{eventSource}: Will enable advanced features for club {specificClubId} from metadata");
        }

        // If club initials provided, look up the club
        if (!string.IsNullOrWhiteSpace(clubInitialsFromMetadata))
        {
            try
            {
                var club = await _clubService.GetClubByIdAsync(
                    await _clubService.GetClubId(clubInitialsFromMetadata));
                if (club != null)
                {
                    clubIdsToEnable.Add(club.Id);
                    logger.LogInformation($"{eventSource}: Will enable advanced features for club {club.Id} ({clubInitialsFromMetadata}) from metadata");
                }
                else
                {
                    logger.LogWarning($"{eventSource}: Club with initials '{clubInitialsFromMetadata}' not found");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"{eventSource}: Error looking up club by initials '{clubInitialsFromMetadata}'");
            }
        }

        // Also get all clubs for the user's email
        if (!string.IsNullOrWhiteSpace(email))
        {
            try
            {
                var userClubIds = await _userService.GetClubIdsForUserEmailAsync(email);
                foreach (var clubId in userClubIds)
                {
                    clubIdsToEnable.Add(clubId);
                }
                logger.LogInformation($"{eventSource}: Found {userClubIds.Count} club(s) for email {email}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"{eventSource}: Error getting clubs for email '{email}'");
            }
        }

        if (clubIdsToEnable.Count == 0)
        {
            logger.LogWarning($"{eventSource}: No clubs found to enable advanced features. Email={email}, ClubId={clubIdFromMetadata}, ClubInitials={clubInitialsFromMetadata}");
            return;
        }

        // Enable advanced features for all identified clubs
        foreach (var clubId in clubIdsToEnable)
        {
            try
            {
                await _clubService.SetUseAdvancedFeaturesAsync(clubId, true);
                
                // Set the subscription type if provided
                if (!string.IsNullOrWhiteSpace(subscriptionType))
                {
                    await _clubService.SetSubscriptionTypeAsync(clubId, subscriptionType);
                    logger.LogInformation($"Set subscription type to '{subscriptionType}' for club {clubId} via {eventSource}");
                }
                
                logger.LogInformation($"Enabled advanced features for club {clubId} via {eventSource}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to enable advanced features for club {clubId} via {eventSource}");
            }
        }
    }

    private async Task DisableAdvancedFeaturesAsync(
        string email, 
        string clubIdFromMetadata, 
        string clubInitialsFromMetadata, 
        ILogger logger, 
        string eventSource)
    {
        var clubIdsToDisable = new HashSet<Guid>();

        // If specific club ID is provided in metadata, add it
        if (!string.IsNullOrWhiteSpace(clubIdFromMetadata) && Guid.TryParse(clubIdFromMetadata, out var specificClubId))
        {
            clubIdsToDisable.Add(specificClubId);
            logger.LogInformation($"{eventSource}: Will disable advanced features for club {specificClubId} from metadata");
        }

        // If club initials provided, look up the club
        if (!string.IsNullOrWhiteSpace(clubInitialsFromMetadata))
        {
            try
            {
                var club = await _clubService.GetClubByIdAsync(
                    await _clubService.GetClubId(clubInitialsFromMetadata));
                if (club != null)
                {
                    clubIdsToDisable.Add(club.Id);
                    logger.LogInformation($"{eventSource}: Will disable advanced features for club {club.Id} ({clubInitialsFromMetadata}) from metadata");
                }
                else
                {
                    logger.LogWarning($"{eventSource}: Club with initials '{clubInitialsFromMetadata}' not found");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"{eventSource}: Error looking up club by initials '{clubInitialsFromMetadata}'");
            }
        }

        // Also get all clubs for the user's email
        if (!string.IsNullOrWhiteSpace(email))
        {
            try
            {
                var userClubIds = await _userService.GetClubIdsForUserEmailAsync(email);
                foreach (var clubId in userClubIds)
                {
                    clubIdsToDisable.Add(clubId);
                }
                logger.LogInformation($"{eventSource}: Found {userClubIds.Count} club(s) for email {email}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"{eventSource}: Error getting clubs for email '{email}'");
            }
        }

        if (clubIdsToDisable.Count == 0)
        {
            logger.LogWarning($"{eventSource}: No clubs found to disable advanced features. Email={email}, ClubId={clubIdFromMetadata}, ClubInitials={clubInitialsFromMetadata}");
            return;
        }

        // Disable advanced features for all identified clubs
        foreach (var clubId in clubIdsToDisable)
        {
            try
            {
                await _clubService.SetUseAdvancedFeaturesAsync(clubId, false);
                logger.LogInformation($"Disabled advanced features for club {clubId} via {eventSource}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed to disable advanced features for club {clubId} via {eventSource}");
            }
        }
    }

    public async Task<(string ClubId, string ClubInitials)> GetFirstClubForUserEmailAsync(string userEmail)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            return (null, null);

        var clubIds = await _userService.GetClubIdsForUserEmailAsync(userEmail);
        var firstClubId = clubIds.FirstOrDefault();
        if (firstClubId == Guid.Empty)
            return (null, null);

        var club = await _clubService.GetClubByIdAsync(firstClubId);
        if (club == null)
            return (firstClubId.ToString(), null);
        return (club.Id.ToString(), club.Initials);
    }

    public async Task<bool> UserHasMultipleClubsAsync(string userEmail)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            return false;

        var clubIds = await _userService.GetClubIdsForUserEmailAsync(userEmail);
        return clubIds.Count > 1;
    }
}
