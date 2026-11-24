using VCare.SharedKernel.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Staffs.Infrastructure.Persistence;
using VCare.SharedKernel.Domain;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Staffs.Infrastructure
{
    /// <summary>
    /// Rate limits for the routes that anyone can call, per client address, so one
    /// client cannot guess passwords or flood someone's inbox quickly. Generous,
    /// because a whole care home usually signs in from one address at shift change;
    /// per-account lockout is what stops a password being guessed. Behind a proxy,
    /// configure forwarded headers, or every client shares the proxy's address.
    /// </summary>
    public static class AuthRateLimits
    {
        /// <summary>Sign-in, refresh, logout and the links in emails: 60 a minute.</summary>
        public const string SignIn = "auth-sign-in";

        /// <summary>Anything that sends an email or creates an account: 10 every 15 minutes.</summary>
        public const string SendsEmail = "auth-sends-email";
    }

    public static class StaffModule
    {
        /// <summary>
        /// Identity's rules for VCare. Lockout is what stops a password being guessed:
        /// five wrong passwords lock the account for 15 minutes.
        /// </summary>
        public static void ConfigureIdentity(IdentityOptions options)
        {
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.User.RequireUniqueEmail = true;
        }
       
        public static IServiceCollection AddStaffModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<Staffs.Infrastructure.Persistence.StaffDbContext>((sp, options) =>
                options.UseSqlServer(configuration.GetConnectionString("Default"), sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", StaffDbContext.schemaName))
                    .UseVCareInterceptors(sp));

            services.AddScoped<Staffs.Application.Abstraction.IStaffRepository, Staffs.Infrastructure.Repositories.StaffRepository>();
            services.AddScoped<Staffs.Application.Services.Interface.IStaffService, Staffs.Application.Services.StaffService>();
            services.AddScoped<Staffs.Application.Services.Interface.IAuthService, Staffs.Application.Services.AuthService>();
            services.AddScoped<Staffs.Application.Services.Interface.IJwtTokenService, Staffs.Application.Services.TokenService>();
            services.AddScoped<Staffs.Application.Services.Interface.IRefreshTokenRepository, Staffs.Infrastructure.Repositories.RefreshTokenRepository>();
            services.AddScoped<VCare.SharedKernel.Abstractions.IRefreshToken, Staffs.Application.Services.RefreshTokenGenerator>();
            services.AddScoped<Staffs.Application.Services.Interface.IStaffPrincipalReader, StaffPrincipalReader>();

            // Other modules check a staff member exists in the caller's home
            // through this, not by reaching into this module's tables.
            services.AddScoped<VCare.SharedKernel.Abstractions.IStaffDirectory, Staffs.Infrastructure.Repositories.StaffDirectory>();
            var storageConfiguration = configuration.GetSection("Storage");
            services.AddOptions<StorageEntity>().Bind(storageConfiguration).ValidateOnStart().ValidateDataAnnotations();

            services.TryAddSingleton(TimeProvider.System);
            services.AddScoped<StaffSessionValidator>();

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy(AuthRateLimits.SignIn, context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
                options.AddPolicy(AuthRateLimits.SendsEmail, context => RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(15), QueueLimit = 0 }));
            });

            services.AddIdentity<ApplicationUser, ApplicationRole>(ConfigureIdentity)
                .AddEntityFrameworkStores<StaffDbContext>()
                .AddDefaultTokenProviders();
            services.AddAuthentication( Options =>
            {
                Options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                Options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;;
            }).AddJwtBearer(Options =>
            {
                Options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
                };
                // A valid signature only says the token was issued. This asks whether
                // its holder still has that access: not deactivated, and no role change
                // or password reset since it was issued.
                Options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var sessions = context.HttpContext.RequestServices.GetRequiredService<StaffSessionValidator>();
                        if (context.Principal is null
                            || !await sessions.IsCurrentAsync(context.Principal, context.HttpContext.RequestAborted))
                        {
                            context.Fail("This session has ended. Sign in again.");
                        }
                    }
                };
            });

            return services;
        }
    }
}