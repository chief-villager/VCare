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
using System.Data.Common;

namespace Staffs.Infrastructure
{
    public static class StaffModule
    {
       
        public static IServiceCollection AddStaffModule(this IServiceCollection services, IConfiguration configuration)
        {
            // Connection comes from DI, shared with OutboxDbContext so that
            // CommitandSaveAsync can enlist the outbox in its transaction.
            services.AddDbContext<Staffs.Infrastructure.Persistence.StaffDbContext>((sp, options) =>
                options.UseSqlServer(sp.GetRequiredService<DbConnection>(), sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", StaffDbContext.schemaName)));

            services.AddScoped<Staffs.Application.Abstraction.IStaffRepository, Staffs.Infrastructure.Repositories.StaffRepository>();
            services.AddScoped<Staffs.Application.Services.Interface.IStaffService, Staffs.Application.Services.StaffService>();
            services.AddScoped<Staffs.Application.Services.Interface.IAuthService, Staffs.Application.Services.AuthService>();
            services.AddScoped<Staffs.Application.Services.Interface.IJwtTokenService, Staffs.Application.Services.TokenService>();
            services.AddScoped<Staffs.Application.Services.Interface.IRefreshTokenRepository, Staffs.Infrastructure.Repositories.RefreshTokenRepository>();
            services.AddScoped<VCare.SharedKernel.Abstractions.IRefreshToken, Staffs.Application.Services.RefreshTokenGenerator>();
            services.AddScoped<Staffs.Application.Services.Interface.IStaffPrincipalReader, StaffPrincipalReader>();

            // Raised by the staff aggregate; the handler mints the confirmation
            // link and hands it to the outbox rather than sending it inline.
            services.AddScoped<VCare.SharedKernel.Abstractions.IDomainEventHandler<VCare.SharedKernel.Domain.StaffCreatedEvent>,
                Staffs.Infrastructure.Repositories.StaffEventHandler>();
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