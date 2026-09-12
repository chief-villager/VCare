using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;

namespace src.Modules.CareHomes.Domain.Entity
{
    internal class CareHome : AggregateRoot<CareHomeId>
    {
        public string Name {get; private set;} = null!;
        public string Address {get; private set;} = null!;
        public string Email {get; private set;} = null!;


        private CareHome(){}


        public static Result<CareHome> CreateCareHome(string name, string address, string email)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Result.Failure<CareHome>("Name is required");
            }
            if (string.IsNullOrWhiteSpace(address))
            {
                return Result.Failure<CareHome>("Address is required");
            }
            if (string.IsNullOrWhiteSpace(email))
            {
                return Result.Failure<CareHome>("Email is required");
            }
            CareHome careHome = new CareHome
            {
                Id = CareHomeId.New(),
                Name = name,
                Address = address,
                Email = email
            };
            return Result.Success(careHome);

        }

        public Result UpdateCareHome(string? name, string? address, string? email)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                Name=name;
            }
            if (!string.IsNullOrWhiteSpace(address))
            {
                Address=address;
            }
            if (!string.IsNullOrWhiteSpace(email))
            {
                Email=email;
            }
            return Result.Success();
        }
       
    }
}