using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Results;

namespace VCare.SharedKernel.Abstractions
{
    public interface IOutboxWriter 
    {
        Task<Result>AddOutboxMessageAsync<TPayload>(TPayload payload ,string Email,string UserName, 
        string eventType,string url, CancellationToken cancellationToken)  where TPayload:IOutboxPayload;
    }
}