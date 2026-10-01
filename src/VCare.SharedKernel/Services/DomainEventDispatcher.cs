using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;

namespace VCare.SharedKernel.Services
{
    public class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
    {
        public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default)
        {
            foreach (var domainEvent in domainEvents)
            {
                var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
                var handlers = serviceProvider.GetServices(handlerType);
                foreach (var handler in handlers)
                {
                    if (handler is null)
                    {
                        continue;
                    }
                    var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
                    await (Task)method.Invoke(handler, [domainEvent, ct])!;
                }
            }
            
        }
    }
}