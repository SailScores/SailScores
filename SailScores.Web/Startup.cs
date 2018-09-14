using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SailScores.Web.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using SailScores.Core.Services;
using SailScores.Database;
using AutoMapper;
using SailScores.Web;
using Microsoft.Extensions.Caching.Memory;
using SailScores.Web.Services;

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

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "SailScores API", Version = "v1" });
                c.IncludeXmlComments(string.Format(@"{0}\SailScores.Web.xml",
                     System.AppDomain.CurrentDomain.BaseDirectory));
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });


            services.AddDbContext<SailScoresIdentityContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));

            services.AddDefaultIdentity<IdentityUser>()
                .AddEntityFrameworkStores<SailScoresIdentityContext>();

            services.AddTransient<IEmailSender, EmailSender>();
            // Make sure API calls that require auth return 401, not redirect on auth failure.
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = new PathString("/Account/Login");
                options.LogoutPath = new PathString("/Account/Logout");

                options.Events.OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api")
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

            services.AddDbContext<SailScoresContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddAutoMapper();

            RegisterSailScoresServices(services);


        }

        private void RegisterSailScoresServices(IServiceCollection services)
        {
            services.AddScoped<IClubService, ClubService>();
            services.AddDbContext<ISailScoresContext, SailScoresContext>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });


            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "areas",
                    template: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
                routes.MapRoute(
                    name: "ClubRoute",
                    template: "{clubInitials}/{controller}/{action}",
                    defaults: new { controller = "Club", action = "Index" },
                    constraints: new
                    {
                        clubInitials = new ClubRouteConstraint(() =>
                            app.ApplicationServices.CreateScope().ServiceProvider.GetRequiredService<ISailScoresContext>(),
                            app.ApplicationServices.CreateScope().ServiceProvider.GetRequiredService<IMemoryCache>()
                        )
                    });
                routes.MapRoute(
                    name: "Series",
                    template: "{clubInitials}/{season}/{seriesName}",
                    defaults: new { controller = "Series", action = "Details" },
                    constraints: new
                    {
                        clubInitials = new ClubRouteConstraint(() =>
                                app.ApplicationServices.CreateScope().ServiceProvider.GetRequiredService<ISailScoresContext>(),
                            app.ApplicationServices.CreateScope().ServiceProvider.GetRequiredService<IMemoryCache>()
                        )
                    });
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
