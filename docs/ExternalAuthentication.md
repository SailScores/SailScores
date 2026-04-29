# External Authentication — Setup & Testing Guide

SailScores supports four external OAuth / OIDC identity providers in addition to
local email/password accounts.  Providers are **optional**: a provider is only
registered (and the button appears on the login page) when its `ClientId` /
`AppId` is present in configuration.

---

## Table of contents

1. [Architecture overview](#architecture-overview)
2. [Configuration](#configuration)
   - [Storing secrets safely](#storing-secrets-safely)
   - [Google](#google)
   - [Microsoft Account](#microsoft-account)
   - [Apple (Sign in with Apple)](#apple-sign-in-with-apple)
   - [Facebook](#facebook)
3. [Redirect / callback URLs per environment](#redirect--callback-urls-per-environment)
4. [How to switch between environments](#how-to-switch-between-environments)
5. [Testing authentication](#testing-authentication)
6. [Account linking behaviour](#account-linking-behaviour)
7. [Name and email handling](#name-and-email-handling)

---

## Architecture overview

```
Browser
  │  POST /Account/ExternalLogin?provider=Google
  ▼
AccountController.ExternalLogin()
  │  Challenge(properties, "Google")
  ▼
Google OAuth consent screen
  │  redirects to /signin-google (ASP.NET Core middleware)
  ▼
AccountController.ExternalLoginCallback()
  ├─ ExternalLoginSignInAsync → success → update profile → redirect
  ├─ FindByEmailAsync → auto-link → sign in → redirect
  └─ no existing account → ExternalLogin.cshtml (confirm / complete profile)
       │  POST /Account/ExternalLoginConfirmation
       ▼
     Create ApplicationUser → AddLoginAsync → SignInAsync
```

All four providers follow this same flow via ASP.NET Core Identity's built-in
`SignInManager<T>` / `UserManager<T>` APIs.  Existing local accounts are **never
modified** by this feature; the external login is simply _added_ as an additional
login record in `AspNetUserLogins`.

---

## Configuration

### Storing secrets safely

**Never commit real credentials to source control.**

| Environment | Storage mechanism |
|-------------|-------------------|
| Local development | [.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) |
| Staging / Production | Azure App Service → Configuration → Application Settings |

#### Local secrets — quick-start commands

```powershell
# Run from the SailScores.Web directory, or pass --project SailScores.Web
dotnet user-secrets init

# Google
dotnet user-secrets set "Authentication:Google:ClientId"     "<your-client-id>"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<your-client-secret>"

# Microsoft
dotnet user-secrets set "Authentication:Microsoft:ClientId"     "<your-client-id>"
dotnet user-secrets set "Authentication:Microsoft:ClientSecret" "<your-client-secret>"

# Apple
dotnet user-secrets set "Authentication:Apple:ClientId"    "com.sailscores.auth"
dotnet user-secrets set "Authentication:Apple:TeamId"      "<10-char-team-id>"
dotnet user-secrets set "Authentication:Apple:KeyId"       "<10-char-key-id>"
dotnet user-secrets set "Authentication:Apple:PrivateKey"  "<full PEM content of .p8 file>"

# Facebook
dotnet user-secrets set "Authentication:Facebook:AppId"     "<your-app-id>"
dotnet user-secrets set "Authentication:Facebook:AppSecret" "<your-app-secret>"
```

For Azure App Service use the same key names (replacing `:` with `__` when
required by the Azure portal):

```
Authentication__Google__ClientId     = <value>
Authentication__Google__ClientSecret = <value>
...
```

---

### Google

#### Register the application

1. Go to [Google Cloud Console → APIs & Services → Credentials](https://console.cloud.google.com/apis/credentials).
2. **Create credentials → OAuth 2.0 Client ID**.
3. Application type: **Web application**.
4. Add **Authorised redirect URIs** (see table below).
5. Enable the **Google People API** in the same project so profile/name claims
   are included in the token response.
6. Copy the **Client ID** and **Client Secret**.

#### Required OAuth scopes

`openid`, `email`, `profile` — the handler requests these automatically.

#### Claim mapping

| Google claim | ASP.NET Core ClaimType |
|---|---|
| `email` | `ClaimTypes.Email` |
| `given_name` | `ClaimTypes.GivenName` |
| `family_name` | `ClaimTypes.Surname` |

---

### Microsoft Account

#### Register the application

1. Go to [Azure Portal → App registrations](https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps).
2. **New registration**.
3. Supported account types: **Accounts in any organizational directory (Any Azure
   AD directory - Multitenant) and personal Microsoft accounts** — this allows
   both personal (MSA) and work/school (Entra ID) sign-in.
4. Add **Redirect URIs** (Platform: Web) — see table below.
5. Under **Certificates & secrets → Client secrets**, create a new secret.
6. Copy the **Application (client) ID** and the secret value.

#### Claim mapping

| Microsoft claim | ASP.NET Core ClaimType |
|---|---|
| `email` / `preferred_username` | `ClaimTypes.Email` |
| `given_name` | `ClaimTypes.GivenName` |
| `family_name` | `ClaimTypes.Surname` |

---

### Apple (Sign in with Apple)

Apple Sign In has several requirements that differ from the other providers.

#### Prerequisites

- An active [Apple Developer](https://developer.apple.com) membership.
- Your domains must be **verified** in the Apple Developer portal
  (staging + production; localhost is not allowed).
- The callback URL **must use HTTPS**.  For local development use
  [ngrok](https://ngrok.com) or another HTTPS tunnel and register that URL.

#### Register the application

1. Log in to [Apple Developer → Certificates, Identifiers & Profiles](https://developer.apple.com/account/resources).
2. **Identifiers → App IDs → +** — create an App ID and enable
   **Sign in with Apple**.
3. **Identifiers → Services IDs → +** — create a Services ID
   (e.g. `com.sailscores.auth`).  This becomes your **ClientId**.
   - Enable **Sign in with Apple** on the Services ID.
   - Configure domains and return URLs (see table below).
4. **Keys → +** — create a key, enable **Sign in with Apple**, and click
   **Configure**.  Associate it with the App ID created above.
   - Download the `.p8` private-key file **once** (it cannot be re-downloaded).
   - Note the **Key ID** (10-character string).
5. Note your **Team ID** (visible in the top-right of the Developer portal).

#### Storing the private key

The full PEM content of the `.p8` file (including `-----BEGIN PRIVATE KEY-----`
header and footer) must be stored in configuration:

```
Authentication:Apple:PrivateKey = -----BEGIN PRIVATE KEY-----\nMIGH...\n-----END PRIVATE KEY-----
```

In User Secrets / Azure App Settings store the value as a single string with
literal `\n` or actual line breaks — both are accepted by the handler.

#### Domain verification

Apple requires that you serve a domain-verification file at:

```
https://<yourdomain>/.well-known/apple-developer-domain-association.txt
```

The SailScores application already serves the `.well-known` folder
(see `Startup.cs` static-files middleware).  Place the file downloaded from
Apple in the `SailScores.Web/.well-known/` directory and commit it.

#### Important Apple-specific behaviour

- **Name is only sent on the first sign-in.**  The application captures and
  stores it at that moment.  On subsequent sign-ins the name claims are absent,
  so the locally stored values are preserved.
- Users may opt to use an Apple relay email address.  This is stored as-is and
  is valid for email delivery.

#### Claim mapping

| Apple JWT claim | ASP.NET Core ClaimType |
|---|---|
| `email` | `ClaimTypes.Email` |
| `given_name` (first login only) | `ClaimTypes.GivenName` |
| `family_name` (first login only) | `ClaimTypes.Surname` |

---

### Facebook

#### Register the application

1. Go to [Facebook Developers → My Apps](https://developers.facebook.com/apps).
2. Create an app (or use an existing one) of type **Consumer** or **None**.
3. Add the **Facebook Login** product.
4. Under **Facebook Login → Settings**, add Valid OAuth Redirect URIs
   (see table below).
5. Under **App Settings → Basic**, note the **App ID** and **App Secret**.
6. Request the `email` and `public_profile` permissions
   (approved by default for basic use).
7. Set the app to **Live** mode so real users (not just test accounts) can log in.

#### Claim mapping

| Facebook Graph field | ASP.NET Core ClaimType |
|---|---|
| `email` | `ClaimTypes.Email` |
| `first_name` | `ClaimTypes.GivenName` |
| `last_name` | `ClaimTypes.Surname` |

---

## Redirect / callback URLs per environment

Register **all three** redirect URIs in each provider's developer portal before
testing.  The default ASP.NET Core callback paths are used throughout.

| Environment | Base URL | Google | Microsoft | Apple | Facebook |
|---|---|---|---|---|---|
| **Local** | `https://localhost:5001` | `/signin-google` | `/signin-microsoft` | `/signin-apple` ¹ | `/signin-facebook` |
| **Staging** | `https://stage.sailscores.com` | `/signin-google` | `/signin-microsoft` | `/signin-apple` | `/signin-facebook` |
| **Production** | `https://www.sailscores.com` | `/signin-google` | `/signin-microsoft` | `/signin-apple` | `/signin-facebook` |

> ¹ Apple does **not** support `localhost` redirect URIs.  Use an ngrok tunnel
>   (e.g. `https://abcd1234.ngrok.io`) for local development and register that URL.

Full example for production:

```
https://www.sailscores.com/signin-google
https://www.sailscores.com/signin-microsoft
https://www.sailscores.com/signin-apple
https://www.sailscores.com/signin-facebook
```

---

## How to switch between environments

ASP.NET Core configuration is layered.  The application reads, in order:

1. `appsettings.json` — shared defaults (contains **empty** credential values)
2. `appsettings.{Environment}.json` — environment-specific overrides
3. User Secrets — local developer secrets (not committed to source control)
4. Environment variables — Azure App Service Application Settings

To activate a provider **only** in production but not locally, simply leave
User Secrets empty for that provider and populate the Azure App Service
Application Settings instead.

To run with staging credentials locally:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Staging"
dotnet run --project SailScores.Web
```

and populate `appsettings.Staging.json` (git-ignored if it contains secrets) or
User Secrets with the staging provider credentials.

---

## Testing authentication

### General checklist

- [ ] Provider registered with all required redirect URIs for the target
      environment.
- [ ] `ClientId` / `AppId` present in the relevant configuration layer.
- [ ] Application running on HTTPS (required by all providers).
- [ ] For Facebook: app is in **Live** mode (or the test account is added as a
      developer / tester on the app).
- [ ] For Apple: domain verification file is served at `/.well-known/...`.

### Testing each provider locally

#### Google & Microsoft & Facebook (localhost)

These providers allow `localhost` redirect URIs.  Add
`https://localhost:5001/signin-<provider>` to the respective developer portal,
set credentials in User Secrets, and run:

```powershell
cd SailScores.Web
dotnet run
```

Navigate to `https://localhost:5001/Account/Login` and click the provider button.

#### Apple (requires HTTPS tunnel)

1. Install [ngrok](https://ngrok.com) and start a tunnel:
   ```powershell
   ngrok http https://localhost:5001
   ```
2. Note the HTTPS forwarding URL (e.g. `https://abcd1234.ngrok.io`).
3. Add `https://abcd1234.ngrok.io/signin-apple` as a return URL in the Apple
   Services ID configuration.
4. Update your local `launchSettings.json` or use the `App:BaseUrl` config key
   to match the ngrok URL if needed for domain-verification purposes.
5. Set credentials in User Secrets and run the application.

### Verifying account linking

1. Register a local account with an email address that you also use with
   an external provider.
2. Sign out, then click the external provider button and authenticate.
3. Expect: the external login is automatically linked to the existing account
   and you are signed in — **no duplicate account is created**.
4. Verify in the database: `AspNetUserLogins` should contain a row for that
   user with the provider name and provider key.

### Verifying name population

1. Sign in with an external provider for the first time using a new account.
2. Expect: `ApplicationUser.FirstName` and `LastName` are populated from
   the provider's claims.
3. For Apple second-login: sign out and sign back in with Apple — name fields
   should be unchanged (populated from the first login).
4. If name was not available, you should be redirected to the Update Profile
   page immediately after account creation.

### Verifying profile refresh

1. Sign in with an external provider that has already been linked.
2. If the provider supplies a different first or last name (e.g. after a name
   change), the local record is updated automatically.

---

## Account linking behaviour

| Scenario | Behaviour |
|---|---|
| External login exists for that provider + key | Sign in immediately; refresh name from claims |
| Same email exists as local account, no external login | Auto-link; sign in; refresh name from claims |
| No existing account | Show `ExternalLogin.cshtml` confirmation page; create new `ApplicationUser` |
| Name not supplied by provider | Placeholder used; user redirected to Update Profile |

Links are stored in the standard ASP.NET Core Identity table `AspNetUserLogins`.
A user can have multiple external logins (e.g. Google **and** Facebook) linked to
the same `ApplicationUser`.

---

## Name and email handling

SailScores requires `FirstName`, `LastName`, and `Email` to be present on every
`ApplicationUser` record (used for permissions checks and "last updated by"
metadata).

| Scenario | Resolution |
|---|---|
| Provider supplies `given_name` + `family_name` | Used directly |
| Provider supplies only `name` (full name) | Split on first space: `[0]` → FirstName, `[1…]` → LastName |
| Provider supplies no name (e.g. Apple after first login) | Existing stored values are retained |
| Truly no name available | Placeholder values used; user redirected to Update Profile |
| Apple relay email | Stored as-is; valid for email delivery |
