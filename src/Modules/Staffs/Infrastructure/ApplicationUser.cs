using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using VCare.SharedKernel.Domain;

namespace Staffs.Infrastructure
{
    internal class ApplicationUser : IdentityUser<Guid>,IHasDomainEvents
    {
        private readonly List<IDomainEvent> _domainEvents = [];
        public Guid CareHomeId {get; set;}

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        public void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}