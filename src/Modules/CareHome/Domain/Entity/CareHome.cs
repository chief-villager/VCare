using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;

namespace src.Modules.CareHome.Domain.Entity
{
    internal class CareHome : AggregateRoot<CareHomeId>
    {
        public string Name {get; private set;} = null!;
        public string Address {get; private set;} = null!;


        private CareHome(){}


        public static Result<CareHome> CreateCareHome(string name, string address)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Result.Failure("Name is required");
            }
            if (string.IsNullOrWhiteSpace(address))
            {
                Result.Failure("Address is required");
            }
            CareHome careHome = new CareHome
            {
                Id = CareHomeId.New(),
                Name = name,
                Address = address
            };
            return Result.Success(careHome);

        }
       
    }
}