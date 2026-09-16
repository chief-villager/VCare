using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Staffs.Application.Services.Interface;
using Staffs.Domain.Entity;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace Staffs.Application.Services
{
    public class RefreshTokenGenerator( IJwtTokenService tokenService, 
    IRefreshTokenRepository refreshTokenRepository) : IRefreshToken
    {
        private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromHours(1);
        private const int SecretBytes = 32;

        // Every rejection says the same thing. Distinguishing "unknown", "expired"
        // and "revoked" to the caller tells a probing client which of its guesses
        // was closest.
        private const string InvalidToken = "Invalid Token";

        public async Task<Result<(string AccessToken, string RefreshToken)>> CreateTokensAsync(string name, Guid tokenFamily,
        StaffId staffId, Guid careHomeId, IEnumerable<string> roles, CancellationToken cancellationToken)
        {
            var accessToken = tokenService.CreateToken(name, staffId, careHomeId, roles);
            if (accessToken.IsFailure)
                return Result.Failure<(string, string)>(accessToken.Error);

            // Fresh entropy per call. Held in a field - static or instance - this
            // would hand the same refresh token to every caller for the lifetime
            // of the process, and any staff member could refresh as any other.
            var secret = RandomNumberGenerator.GetBytes(SecretBytes);
            var issuedAt = DateTime.UtcNow;

            var refreshToken = RefreshToken.Create(
                tokenHash: SHA256.HashData(secret),
                tokenFamily: tokenFamily,
                createdTime: issuedAt,
                expirationDate: issuedAt.Add(RefreshTokenLifetime));

            if (refreshToken.IsFailure)
                return Result.Failure<(string, string)>(refreshToken.Error);

            await refreshTokenRepository.AddAsync(refreshToken.Value, cancellationToken);
            await refreshTokenRepository.SaveChangesAsync(cancellationToken);

            // The only time the token itself exists outside this method. Naming it
            // is the response DTO's job: a label baked into the value cannot be
            // sent back and fails to hex-decode.
            return Result.Success((accessToken.Value, Convert.ToHexString(secret)));
        }

        public async Task<Result<(string AccessToken, string RefreshToken)>> RefreshTokenAsync(string currentRefreshToken,string name,
        StaffId staffId, Guid careHomeId,IEnumerable<string> roles, CancellationToken cancellationToken)
        {
            if (!TryReadSecret(currentRefreshToken, out var secret))
                return Result.Failure<(string, string)>(InvalidToken);

            // Looked up by hash, so the plaintext never has to be stored to be
            // matched, and an unknown token is simply a miss.
            var token = await refreshTokenRepository.GetByHashAsync(SHA256.HashData(secret), cancellationToken);
            if (token is null)
                return Result.Failure<(string, string)>(InvalidToken);

            var now = DateTime.UtcNow;

            // A revoked token coming back means it was captured: the legitimate
            // holder already rotated it. Nothing in the family can be trusted.
            if (token.IsRevoked)
            {
                await RevokeFamilyAsync(token.TokenFamily, now, cancellationToken);
                return Result.Failure<(string, string)>(InvalidToken);
            }

            if (token.IsExpired(now))
                return Result.Failure<(string, string)>(InvalidToken);

            var revoked = token.RevokeToken(now);
            if (revoked.IsFailure)
                return Result.Failure<(string, string)>(InvalidToken);

            // The replacement stays in the family it came from, so a later reuse
            // of any token in the chain revokes the whole chain.
            return await CreateTokensAsync(name, token.TokenFamily, staffId, careHomeId, roles, cancellationToken);
        }

        public async Task<Result> RevokeFamilyAsync(string currentRefreshToken, CancellationToken cancellationToken)
        {
            if (!TryReadSecret(currentRefreshToken, out var secret))
                return Result.Failure(InvalidToken);

            var token = await refreshTokenRepository.GetByHashAsync(SHA256.HashData(secret), cancellationToken);
            if (token is null)
                return Result.Failure(InvalidToken);

            // An already-revoked token still names its family, so logging out with
            // a stale token finishes the job rather than failing.
            await RevokeFamilyAsync(token.TokenFamily, DateTime.UtcNow, cancellationToken);
            return Result.Success();
        }

        private async Task RevokeFamilyAsync(Guid tokenFamily, DateTime utcNow, CancellationToken cancellationToken)
        {
            var family = await refreshTokenRepository.GetAllActiveTokenAsync(tokenFamily, cancellationToken);
            foreach (var item in family)
            {
                item.RevokeToken(utcNow);
            }
            await refreshTokenRepository.SaveChangesAsync(cancellationToken);
        }

        // The token is untrusted input: anything that is not exactly SecretBytes
        // of hex is rejected before it reaches the database.
        private static bool TryReadSecret(string currentRefreshToken, out byte[] secret)
        {
            secret = [];
            if (string.IsNullOrWhiteSpace(currentRefreshToken))
                return false;

            try
            {
                secret = Convert.FromHexString(currentRefreshToken);
            }
            catch (FormatException)
            {
                return false;
            }

            return secret.Length == SecretBytes;
        }
    }
}
