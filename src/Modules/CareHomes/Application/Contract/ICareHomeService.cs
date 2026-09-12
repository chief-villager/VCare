using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using src.Modules.CareHomes.Application.Contract.Dto;
using VCare.SharedKernel.Results;

namespace src.Modules.CareHomes.Application.Contract
{
    internal interface ICareHomeService
    {
        Task<Result<string>> RegisterCareHomeAsync(CreateCareHomeRequest request, CancellationToken cancellationToken);
        Task<Result> UpdateCareHomeAsync(Guid careHomeId, UpdateCareHomeRequest request, CancellationToken cancellationToken);
    }
}
