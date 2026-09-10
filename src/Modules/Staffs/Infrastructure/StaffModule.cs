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

namespace Staffs.Infrastructure
{
    public static class StaffModule
    {
       
        public static IServiceCollection AddStaffModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<Staffs.Infrastructure.Persistence.StaffDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("Default"), sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", StaffDbContext.schemaName)));

            services.AddScoped<Staffs.Application.Abstraction.IStaffRepository, Staffs.Infrastructure.Repositories.StaffRepository>();
            services.AddScoped<Staffs.Application.Services.Interface.IStaffService, Staffs.Application.Services.StaffService>();
            services.AddScoped<Staffs.Application.Services.Interface.IAuthService, Staffs.Application.Services.AuthService>();
            services.AddScoped<Staffs.Application.Services.Interface.IJwtTokenService, Staffs.Application.Services.TokenService>();
            services.AddIdentity<ApplicationUser, ApplicationRole>()
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
            });

            return services;
        }

      

       
    }
}