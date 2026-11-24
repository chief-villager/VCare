using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;

namespace VCare.SharedKernel.Abstractions
{
    public interface INotification
    {
       /// <summary>Sends the email verification link now.</summary>
       Task<Result> SendEmailVerificationLinkAsync(string email, string userName, string url, CancellationToken cancellationToken);

       /// <summary>Sends the password reset link now.</summary>
       Task<Result> SendPasswordResetLinkAsync(string email, string url, CancellationToken cancellationToken);
    }
}
