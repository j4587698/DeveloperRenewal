using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DeveloperRenewal.Entity;
using DeveloperRenewal.Utils;
using GraphLib.Utils;
using LiteDB;
using LiteDB.Identity.Extensions;
using Longbow.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
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
            var liteDatabase = new LiteDatabase("Filename=db/user.db;Connection=Shared");
            LiteDbHelper.InitDb(liteDatabase);
            services.AddTaskServices();
            //TaskServicesManager.Init();
            

            services.AddLiteDBIdentity("Filename=db/user.db;Connection=Shared").AddDefaultTokenProviders();
            services.AddDataProtection().PersistKeysToFileSystem(new System.IO.DirectoryInfo(@"/app"));
            services.AddRazorPages();
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
                })
                .AddGitHub(builder =>
                {
                    builder.ClientId = Configuration["Authentication:Github:ClientId"];
                    builder.ClientSecret = Configuration["Authentication:Github:ClientSecret"];
                    builder.Scope.Add("user:email");
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
            var applications = LiteDbHelper.Instance.GetCollection<ApplicationEntity>(nameof(ApplicationEntity)).Find(x => x.AuthorizationStatus && x.IsEnable);
            if (applications != null && applications.Any())
            {
                foreach (var application in applications)
                {
                    SchedulerUtil.AddScheduler(application.Id);
                }
            }
            app.UseForwardedHeaders();
            app.Use(async (context, next) =>
            {
                context.Request.Scheme = "https";
                await next();
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                //app.UseHsts();
            }
            //app.UseHttpsRedirection();
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
