using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Results;

namespace VCare.SharedKernel.Abstractions
{
    public interface IRefreshToken
    {
        /// <summary>
        /// Issues an access token and a refresh token. <paramref name="securityStamp"/>
        /// goes into the access token and is checked on every request, so a role
        /// change, deactivation or password reset ends it. <paramref name="sessionStartedAt"/>
        /// is when the sign-in happened; null for a new sign-in.
        /// </summary>
        Task<Result<(string AccessToken, string RefreshToken)>> CreateTokensAsync(string name, Guid tokenFamily, StaffId staffId,
        Guid careHomeId,IEnumerable<string> roles, string securityStamp, CancellationToken cancellationToken,
        DateTime? sessionStartedAt = null);
        /// <summary>
        /// Rotates a refresh token. The new pair is issued to whoever the presented
        /// token belongs to, which is read from storage, never taken from the caller.
        /// </summary>
        Task<Result<(string AccessToken, string RefreshToken)>>RefreshTokenAsync(string currentRefreshToken, CancellationToken cancellationToken);

        /// <summary>Revokes every live token in the family the given token belongs to.</summary>
        Task<Result> RevokeFamilyAsync(string currentRefreshToken, CancellationToken cancellationToken);

        /// <summary>Ends every session a staff member has: every live refresh token, in every family.</summary>
        Task RevokeAllForStaffAsync(StaffId staffId, CancellationToken cancellationToken);
    }
}