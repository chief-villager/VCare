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
       Task<Result> ProcessEmailNotificationAsync(string payload, string payloadName, CancellationToken cancellationToken);
       //get payloadName and switch on it to detemine the payload type then build the function for each accordinly.

       /// <summary>Sends the email verification link immediately, without the outbox.</summary>
       Task<Result> SendEmailVerificationLinkAsync(string email, string userName, string url, CancellationToken cancellationToken);

       /// <summary>Sends the password reset link immediately, without the outbox.</summary>
       Task<Result> SendPasswordResetLinkAsync(string email, string url, CancellationToken cancellationToken);
    }
}
