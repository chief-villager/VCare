using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Staffs.Infrastructure
{
    internal sealed class ApplicationUser : IdentityUser<Guid>
    {
        public Guid CareHomeId {get; set;}
    }
}