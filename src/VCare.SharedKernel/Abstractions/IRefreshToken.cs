using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Results;

namespace VCare.SharedKernel.Abstractions
{
    public interface IRefreshToken
    {
        Task<Result<(string AccessToken, string RefreshToken)>> CreateTokensAsync(string name, Guid tokenFamily, StaffId staffId,
        Guid careHomeId,IEnumerable<string> roles, CancellationToken cancellationToken);
        /// <summary>
        /// Rotates a refresh token. The new pair is issued to whoever the presented
        /// token belongs to, which is read from storage, never taken from the caller.
        /// </summary>
        Task<Result<(string AccessToken, string RefreshToken)>>RefreshTokenAsync(string currentRefreshToken, CancellationToken cancellationToken);

        /// <summary>Revokes every live token in the family the given token belongs to.</summary>
        Task<Result> RevokeFamilyAsync(string currentRefreshToken, CancellationToken cancellationToken);
    }
}