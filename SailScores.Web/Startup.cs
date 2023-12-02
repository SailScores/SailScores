using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using SailScores.Core.Extensions;
using SailScores.Core.JobQueue;
using SailScores.Core.Mapping;
using SailScores.Database;
using SailScores.Identity.Entities;
using SailScores.Web.Data;
using SailScores.Web.Extensions;
using SailScores.Web.Mapping;
using SailScores.Web.Services;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Reflection;
using System.Text;
using SailScores.Web.Services.Interfaces;
using WebMarkupMin.AspNetCore3;

namespace SailScores.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLocalization(opts => { opts.ResourcesPath = "Resources"; });

            services.Configure<RequestLocalizationOptions>(options =>
            {
                options.SupportedCultures = new List<CultureInfo> {
                    new CultureInfo("en-US"),
                    new CultureInfo("fi-FI"),
                    new CultureInfo("sv-FI"),
                    new CultureInfo("en-AU"),
                    new CultureInfo("en-IE"),
                };
                options.SupportedUICultures = new List<CultureInfo> {
                    new CultureInfo("en-US"),
                    new CultureInfo("fi-FI"),
                    new CultureInfo("sv-FI"),
                    new CultureInfo("en-AU"),
                    new CultureInfo("en-IE"),
                };

                options.RequestCultureProviders = new List<IRequestCultureProvider>
                { new ClubCultureProvider()
                };
            });

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "SailScores API", Version = "v1" });
                c.IncludeXmlComments(string.Format(
                    CultureInfo.InvariantCulture,
                    @"{0}/SailScores.Web.xml",
                    System.AppDomain.CurrentDomain.BaseDirectory));
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Description = "JWT Authorization header using the Bearer scheme.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.None;
            });


            services.AddDbContext<SailScoresIdentityContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<SailScoresIdentityContext>()
                .AddDefaultTokenProviders();

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            services
                .AddAuthentication()
                .AddCookie(options => options.SlidingExpiration = true)
                .AddJwtBearer(cfg =>
                {
                    cfg.RequireHttpsMetadata = false;
                    cfg.SaveToken = true;
                    cfg.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = Configuration["JwtIssuer"],
                        ValidAudience = Configuration["JwtIssuer"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["JwtKey"])),

                        ClockSkew = TimeSpan.Zero // remove delay of token when expire
                    };
                });


            // Make sure API calls that require auth return 401, not redirect on auth failure.
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = new PathString("/Account/Login");
                options.LogoutPath = new PathString("/Account/Logout");

                options.Events.OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api", StringComparison.InvariantCulture)
                        && context.Response.StatusCode == StatusCodes.Status200OK)
                    {
                        context.Response.Clear();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }
                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            });

            services.AddApplicationInsightsTelemetry(Configuration);

            services.AddDbContext<SailScoresContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));

            services
                .AddMvc(option =>
                {
                    option.Filters.Add(new ResponseCacheAttribute() { NoStore = true, Location = ResponseCacheLocation.None });
                });

            services.AddSingleton<IEmailConfiguration>(Configuration.GetSection("EmailConfiguration").Get<EmailConfiguration>());
            services.AddTransient<IEmailSender, EmailSender>();
            services.AddAutoMapper(
                new[] {
                    typeof(DbToModelMappingProfile).GetTypeInfo().Assembly,
                    typeof(ToViewModelMappingProfile).GetTypeInfo().Assembly
                });

            services.AddHttpClient();

            services.AddWebMarkupMin(
                options =>
                {
                    options.AllowMinificationInDevelopmentEnvironment = true;
                    options.AllowCompressionInDevelopmentEnvironment = true;
                })
                .AddHtmlMinification(
                    options =>
                    {
                    })
                .AddHttpCompression();

            RegisterSailScoresServices(services);

            RegisterBackgroundQueueServices(services);

        }

        private void RegisterBackgroundQueueServices(IServiceCollection services)
        {
            services.AddHostedService<QueuedHostedService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
        }

        private void RegisterSailScoresServices(IServiceCollection services)
        {
            services.RegisterCoreSailScoresServices();
            services.RegisterWebSailScoresServices();

            services.AddDbContext<ISailScoresContext, SailScoresContext>();
        }



        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IMapper mapper)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();

                // Enable middleware to serve generated Swagger as a JSON endpoint.
                app.UseSwagger();

                // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
                // specifying the Swagger JSON endpoint.
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SailScores API V1");
                });
            }
            else
            {
                app.UseStatusCodePagesWithReExecute("/error/{0}");
                app.UseExceptionHandler("/error");
                app.UseHsts();

                app.Use((context, next) =>
                {
                    context.SetEndpoint(null);
                    return next();
                });
            }

            mapper.ConfigurationProvider.AssertConfigurationIsValid();


            app.UseRequestLocalization();

            app.UseHttpsRedirection();
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    const int durationInSeconds = 60 * 60 * 24;
                    ctx.Context.Response.Headers[HeaderNames.CacheControl] =
                        "public,max-age=" + durationInSeconds;
                }
            });
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), ".well-known")),
                RequestPath = "/.well-known",
                ServeUnknownFileTypes = true
            });

            app.UseCookiePolicy();

            app.UseWebMarkupMin();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                var clubRouteConstraint = new ClubRouteConstraint(() =>
                        app.ApplicationServices.CreateScope().ServiceProvider.GetRequiredService<ISailScoresContext>(),
                    app.ApplicationServices.CreateScope().ServiceProvider.GetRequiredService<IMemoryCache>()
                );
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapAreaControllerRoute(
                    "areas",
                    "api",
                    "api/{controller=Home}/{action=Index}/{id?}");
                endpoints.MapControllerRoute(
                    name: "ClubRoute",
                    pattern: "{clubInitials}/{controller}/{action}/{id?}",
                    defaults: new { controller = "Club", action = "Index" },
                    constraints: new { clubInitials = clubRouteConstraint });

                endpoints.MapControllerRoute(
                    name: "Competitor",
                    pattern: "{clubInitials}/Competitor/{sailNumber}/",
                    defaults: new { controller = "Competitor", action = "Details" },
                    constraints: new { clubInitials = clubRouteConstraint });
                endpoints.MapControllerRoute(
                    name: "Race",
                    pattern: "{clubInitials}/Race/{seasonName}",
                    defaults: new { controller = "Race", action = "Index" },
                    constraints: new { clubInitials = clubRouteConstraint });
                endpoints.MapControllerRoute(
                    name: "WhatIf",
                    pattern: "{clubInitials}/Series/WhatIf/{action}/",
                    defaults: new { controller = "WhatIf" },
                    constraints: new { clubInitials = clubRouteConstraint });
                endpoints.MapControllerRoute(
                    name: "Series",
                    pattern: "{clubInitials}/{season}/{seriesName}",
                    defaults: new { controller = "Series", action = "Details" },
                    constraints: new { clubInitials = clubRouteConstraint });
                endpoints.MapControllerRoute(
                    name: "Regatta",
                    pattern: "{clubInitials}/Regatta/{season}/{regattaName}",
                    defaults: new { controller = "Regatta", action = "Details" },
                    constraints: new { clubInitials = clubRouteConstraint });

                endpoints.MapControllerRoute(
                    name: "Error",
                    pattern: "error/{code}",
                    defaults: new { controller = "Error", action = "Error", code = 500 });

                endpoints.MapControllerRoute(
                    "default", "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
