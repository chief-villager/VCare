using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using VCare.SharedKernel.Domain;

namespace Staffs.Infrastructure
{
    internal class ApplicationUser : IdentityUser<Guid>
    {
        public Guid CareHomeId {get; set;}

        // Set when the staff member leaves or is suspended. A deactivated user
        // cannot sign in, refresh, or use a token issued before.
        public DateTimeOffset? DeactivatedAt {get; set;}

      
    }
}