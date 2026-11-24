using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Staffs.Application.Services.Interface;
using Staffs.Domain.Entity;
using VCare.SharedKernel.Abstractions;
using Microsoft.Extensions.Options;
using VCare.SharedKernel.Results;

namespace Staffs.Application.Services
{
    public class RefreshTokenGenerator( IJwtTokenService tokenService, 
    IRefreshTokenRepository refreshTokenRepository, IStaffPrincipalReader principalReader,
    IOptions<JwtSettings> jwtOptions, TimeProvider clock) : IRefreshToken
    {
        private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromHours(1);
        private const int SecretBytes = 32;

        // Every rejection says the same thing. Distinguishing "unknown", "expired"
        // and "revoked" to the caller tells a probing client which of its guesses
        // was closest.
        private static readonly Error InvalidToken = Error.Unauthorized("Auth.InvalidToken", "Invalid Token");

        public async Task<Result<(string AccessToken, string RefreshToken)>> CreateTokensAsync(string name, Guid tokenFamily,
        StaffId staffId, Guid careHomeId, IEnumerable<string> roles, string securityStamp, CancellationToken cancellationToken,
        DateTime? sessionStartedAt = null)
        {
            var accessToken = tokenService.CreateToken(name, staffId, careHomeId, roles, securityStamp);
            if (accessToken.IsFailure)
                return Result.Failure<(string, string)>(accessToken.Error);

            // Fresh entropy per call. Held in a field - static or instance - this
            // would hand the same refresh token to every caller for the lifetime
            // of the process, and any staff member could refresh as any other.
            var secret = RandomNumberGenerator.GetBytes(SecretBytes);
            var issuedAt = clock.GetUtcNow().UtcDateTime;

            var refreshToken = RefreshToken.Create(
                tokenHash: SHA256.HashData(secret),
                tokenFamily: tokenFamily,
                staffId: staffId.Value,
                createdTime: issuedAt,
                expirationDate: issuedAt.Add(RefreshTokenLifetime),
                sessionStartedAt: sessionStartedAt ?? issuedAt);

            if (refreshToken.IsFailure)
                return Result.Failure<(string, string)>(refreshToken.Error);

            await refreshTokenRepository.AddAsync(refreshToken.Value, cancellationToken);
            await refreshTokenRepository.SaveChangesAsync(cancellationToken);

            // The only time the token itself exists outside this method. Naming it
            // is the response DTO's job: a label baked into the value cannot be
            // sent back and fails to hex-decode.
            return Result.Success((accessToken.Value, Convert.ToHexString(secret)));
        }

        public async Task<Result<(string AccessToken, string RefreshToken)>> RefreshTokenAsync(string currentRefreshToken,
        CancellationToken cancellationToken)
        {
            if (!TryReadSecret(currentRefreshToken, out var secret))
                return Result.Failure<(string, string)>(InvalidToken);

            // Looked up by hash, so the plaintext never has to be stored to be
            // matched, and an unknown token is simply a miss.
            var token = await refreshTokenRepository.GetByHashAsync(SHA256.HashData(secret), cancellationToken);
            if (token is null)
                return Result.Failure<(string, string)>(InvalidToken);

            var now = clock.GetUtcNow().UtcDateTime;

            // A revoked token coming back means it was captured: the legitimate
            // holder already rotated it. Nothing in the family can be trusted.
            if (token.IsRevoked)
            {
                await RevokeFamilyAsync(token.TokenFamily, now, cancellationToken);
                return Result.Failure<(string, string)>(InvalidToken);
            }

            if (token.IsExpired(now))
                return Result.Failure<(string, string)>(InvalidToken);

            if (HasOutlivedSession(token, now))
            {
                await RevokeFamilyAsync(token.TokenFamily, now, cancellationToken);
                return Result.Failure<(string, string)>(InvalidToken);
            }

            // Who the replacement is for is settled by the stored row, so a caller
            // cannot rotate someone else's session into a token of their choosing.
            var staffId = new StaffId(token.StaffId);
            var principal = await principalReader.GetByStaffIdAsync(staffId, cancellationToken);
            if (principal.IsFailure)
                return Result.Failure<(string, string)>(InvalidToken);

            var revoked = token.RevokeToken(now);
            if (revoked.IsFailure)
                return Result.Failure<(string, string)>(InvalidToken);

            // The replacement stays in the family it came from, so a later reuse
            // of any token in the chain revokes the whole chain.
            return await CreateTokensAsync(principal.Value.UserName, token.TokenFamily, staffId,
                principal.Value.CareHomeId, principal.Value.Roles, principal.Value.SecurityStamp, cancellationToken,
                token.SessionStartedAt);
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
            await RevokeFamilyAsync(token.TokenFamily, clock.GetUtcNow().UtcDateTime, cancellationToken);
            return Result.Success();
        }

        public async Task RevokeAllForStaffAsync(StaffId staffId, CancellationToken cancellationToken)
        {
            var now = clock.GetUtcNow().UtcDateTime;
            foreach (var item in await refreshTokenRepository.GetAllActiveForStaffAsync(staffId.Value, cancellationToken))
            {
                item.RevokeToken(now);
            }
            await refreshTokenRepository.SaveChangesAsync(cancellationToken);
        }

        // Refreshing keeps a session going one hour at a time; this is where it stops.
        private bool HasOutlivedSession(RefreshToken token, DateTime utcNow) =>
            utcNow - token.SessionStartedAt > TimeSpan.FromHours(Math.Max(1, jwtOptions.Value.MaxSessionHours));

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
