using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Results;

namespace VCare.SharedKernel.Abstractions
{
    public interface IOutboxParticipant
    {
        Task<Result> FlushAsync(DbTransaction transaction, CancellationToken cancellationToken);

    }
}