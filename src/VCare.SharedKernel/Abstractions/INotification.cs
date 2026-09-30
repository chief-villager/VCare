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
    }
}