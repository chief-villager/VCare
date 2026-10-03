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

      
    }
}