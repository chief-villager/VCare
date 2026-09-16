using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Results;

namespace Staffs.Domain.Entity
{
    public class RefreshToken 
    {
        public Guid Id{get; private set;}

        // Only the hash is stored. The token itself is shown to the caller once,
        // at issue: a leaked table must not hand out usable refresh tokens.
        public byte[] TokenHash{get; private set;} = null!;
        public Guid TokenFamily {get; private set;}
        public DateTime ExpirationDate {get; private set;}
        public DateTime CreatedTime {get; private set;}
        public bool IsRevoked {get; private set;}
        public DateTime? RevokedAt {get; private set;}

        private RefreshToken()
        {
            
        }

        public static Result<RefreshToken> Create(byte[] tokenHash, Guid tokenFamily, 
             DateTime createdTime,DateTime expirationDate)
        {
            if (tokenHash is null || tokenHash.Length == 0)
                return Result.Failure<RefreshToken>("A refresh token hash is required.");
            if (tokenFamily == Guid.Empty)
                return Result.Failure<RefreshToken>("A token family is required.");
            if (expirationDate <= createdTime)
                return Result.Failure<RefreshToken>("A refresh token cannot expire before it was created.");

            var token = new RefreshToken
            {
                Id = Guid.NewGuid(),
                TokenHash = tokenHash,
                TokenFamily = tokenFamily,
                CreatedTime = createdTime,
                ExpirationDate = expirationDate,
                IsRevoked = false
                
            };
            return Result.Success(token);
            
        }

        // Expiry and revocation are separate facts. Revoking used to overwrite
        // ExpirationDate, which lost when the token would have lapsed on its own
        // and made an expired token indistinguishable from a revoked one.
        public bool IsExpired(DateTime utcNow) => ExpirationDate <= utcNow;

        public Result RevokeToken(DateTime utcNow)
        {
            if (IsRevoked)
                return Result.Failure("Already revoked token passes in!");

            IsRevoked = true;
            RevokedAt = utcNow;
            return Result.Success();
        }
    
    }
}
