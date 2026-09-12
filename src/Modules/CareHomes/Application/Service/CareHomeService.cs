using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using src.Modules.CareHomes.Application.Contract;
using src.Modules.CareHomes.Application.Contract.Dto;
using src.Modules.CareHomes.Domain.Entity;
using src.Modules.CareHomes.Infrastructure.Persistence;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace src.Modules.CareHomes.Application.Service
{
    internal class CareHomeService(ICareHomeRepository careHomeRepository) : ICareHomeService
    {
        public async Task<Result<string>> RegisterCareHomeAsync(CreateCareHomeRequest request, CancellationToken cancellationToken)
        {
          var careHome = CareHome.CreateCareHome(request.Name, request.Address,request.Email);
          if (careHome.IsFailure)
          {
            return Result.Failure<string>(careHome.Error);
          }
          await careHomeRepository.AddAsync(careHome.Value, cancellationToken);
          await careHomeRepository.SaveChangesAsync(cancellationToken);
          return Result.Success(careHome.Value.Id.ToString());
        }

        public async Task<Result> UpdateCareHomeAsync(Guid careHomeId, UpdateCareHomeRequest request, CancellationToken cancellationToken)
        {

            var careHome = await careHomeRepository.GetByIdAsync(new CareHomeId(careHomeId), cancellationToken);
            if (careHome == null)
            {
                return Result.Failure("carehome does not exist");
            }
            careHome.UpdateCareHome(request.Name,request.Address,request.Email);
            careHomeRepository.UpdateAsync(careHome);
            await careHomeRepository.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }


    }



}
