using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;

namespace Staffs.Infrastructure.Repositories
{
    public class ResetPasswordEventHandler(IOutboxWriter outboxWriter) : IDomainEventHandler<ResetPasswordEvent>
    {
        public Task HandleAsync(ResetPasswordEvent domainEvent, CancellationToken ct = default)
        {
            var payload = new PassWordResetEventPayload(domainEvent.Email, domainEvent.UserName, domainEvent.Url);
            return outboxWriter.AddOutboxMessageAsync(payload, nameof(ResetPasswordEvent), ct);
        }
    }
}