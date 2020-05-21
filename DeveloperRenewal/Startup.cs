using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCore.Identity.LiteDB;
using AspNetCore.Identity.LiteDB.Data;
using AspNetCore.Identity.LiteDB.Models;
using DeveloperRenewal.Entity;
using DeveloperRenewal.Utils;
using GraphLib.Utils;
using LiteDB;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DeveloperRenewal
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
            var liteDatabase = new LiteDatabase("Filename=db\\user.db");
            LiteDbHelper.InitDb(liteDatabase);
            var applications = LiteDbHelper.Instance.GetCollection<ApplicationEntity>(nameof(ApplicationEntity)).Find(x => x.AuthorizationStatus && x.IsEnable);
            if (applications != null && applications.Any())
            {
                foreach (var application in applications)
                {
                    SchedulerUtil.AddScheduler(application.Id);
                }
            }
            services.AddSingleton<LiteDbContext>();
            services.AddSingleton<ILiteDbContext, LiteDbContext>(x => new LiteDbContext(liteDatabase));

            services.AddIdentity<ApplicationUser, AspNetCore.Identity.LiteDB.IdentityRole>(options =>
                {
                    options.Password.RequireDigit = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequiredLength = 6;
                })
                //.AddEntityFrameworkStores<ApplicationDbContext>()
                .AddUserStore<LiteDbUserStore<ApplicationUser>>()
                .AddRoleStore<LiteDbRoleStore<AspNetCore.Identity.LiteDB.IdentityRole>>()
                .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.Cookie.Name = "renewal";
                options.LoginPath = "/User/Login";
                options.LogoutPath = "/User/Logout";
                options.ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
                options.SlidingExpiration = true;
            });
            services.AddAuthentication(configureOptions =>
                {
                    
                })
                .AddCookie(cookieOptions =>
                {
                    cookieOptions.LoginPath = new PathString("/Home/Login");
                    cookieOptions.LogoutPath = new PathString("/Home/Logout");
                    cookieOptions.AccessDeniedPath = new PathString("/Home/Error");
                })
                .AddMicrosoftAccount(microsoftOptions =>
                {
                    microsoftOptions.ClientId = Configuration["Authentication:Microsoft:ClientId"];
                    microsoftOptions.ClientSecret = Configuration["Authentication:Microsoft:ClientSecret"];
                    //microsoftOptions.CallbackPath = new PathString("/signin-microsoft");
                });
            
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
