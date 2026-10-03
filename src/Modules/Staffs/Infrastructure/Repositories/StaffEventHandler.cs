using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Staffs.Application.Services.Interface;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;

namespace Staffs.Infrastructure.Repositories
{
   internal class StaffEventHandler(IOutboxWriter outboxWriter, IAuthService authService) : IDomainEventHandler<StaffCreatedEvent>
    {
        public async Task HandleAsync(StaffCreatedEvent domainEvent, CancellationToken ct = default)
        {
           var confirmationUrl = await authService.GenerateConfirmEmailLink(domainEvent.Email);
           var payload = new StaffCreatedEventPayload(domainEvent.Email, domainEvent.UserName, confirmationUrl.Value);
           await outboxWriter.AddOutboxMessageAsync(payload,nameof(StaffCreatedEvent),ct);
    
        }
    }
}