using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VCare.SharedKernel.Domain
{
   
    public sealed record StaffCreatedEvent(string Email, string UserName) : IDomainEvent;
    public sealed record ResetPasswordEvent(string Email, string UserName, string Url) : IDomainEvent;
    
}