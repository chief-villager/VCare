using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using src.Modules.CareHomes.Application.Contract.Dto;
using VCare.SharedKernel.Results;

namespace src.Modules.CareHomes.Application.Contract
{
    public interface ICareHomeService
    {
        Task<Result<string>> RegisterCareHomeAsync(CreateCareHomeRequest request, CancellationToken cancellationToken);
        Task<Result> UpdateCareHomeAsync(Guid careHomeId, UpdateCareHomeRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Removes a care home. Exists so a caller that creates a care home and then
        /// fails to finish setting it up can undo the part that did commit; the two
        /// modules save to separate contexts and share no transaction.
        /// </summary>
        Task<Result> DeleteCareHomeAsync(Guid careHomeId, CancellationToken cancellationToken);
    }
}
