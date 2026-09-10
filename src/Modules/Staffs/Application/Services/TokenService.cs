using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Staffs.Application.Services.Interface;
using Staffs.Domain;
using Staffs.Domain.Entity;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace Staffs.Application.Services
{
    internal sealed class TokenService(IOptions<JwtSettings> options) : IJwtTokenService
    {
        public Result<string> CreateToken(string name, StaffId staffId, Guid careHomeId, IEnumerable<string> roles)
        {
           var claims = new[]
            {
                new Claim(ClaimTypes.Name, name ),
                new Claim(ClaimTypes.NameIdentifier, staffId.ToString()),
                new Claim("care_home_id", careHomeId.ToString()),
               
            };

            foreach (var role in roles)
            {
                _ = claims.Append(new Claim(ClaimTypes.Role, role));
            }
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Value.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
            issuer: options.Value.Issuer,
            audience: options.Value.Audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(Convert.ToDouble(options.Value.DurationInMinutes)),
            signingCredentials: creds );
            return Result.Success(new JwtSecurityTokenHandler().WriteToken(token));
        }
    }
}