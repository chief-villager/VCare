using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Staffs.Infrastructure
{
    internal class ApplicationRole : IdentityRole<Guid>
    {
    }
}