using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace src.Modules.CareHomes.Application.Contract.Dto
{
    public record  CreateCareHomeRequest( string Name, string Address, string Email);
    public record UpdateCareHomeRequest( string? Name, string? Address, string? Email);
   
   
}