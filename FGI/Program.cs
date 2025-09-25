using FGI.Interfaces;
using FGI.Models;
using FGI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using FGI.Enums;

namespace FGI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddDbContext<AppDbContext>(options =>
  options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<ILeadService, LeadService>();
            builder.Services.AddScoped<ILeadFeedbackService, LeadFeedbackService>();
            builder.Services.AddScoped<IProjectService, ProjectService>();
            builder.Services.AddScoped<IUnitService, UnitService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IHelperService, HelperService>();
            builder.Services.AddScoped<IProfileService, ProfileService>();
            builder.Services.AddScoped<ISettingsService, SettingsService>();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminPolicy", policy =>
                    policy.RequireRole(UserRole.Admin.ToString()));

                options.AddPolicy("MarketingPolicy", policy =>
                    policy.RequireRole(UserRole.Marketing.ToString()));

                options.AddPolicy("SalesPolicy", policy =>
                    policy.RequireRole(UserRole.Sales.ToString()));

                foreach (var role in Enum.GetNames(typeof(UserRole)))
                {
                    options.AddPolicy($"{role}Policy", policy =>
                        policy.RequireRole(role));
                }
            });

            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

            builder.Services.AddHttpContextAccessor();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
