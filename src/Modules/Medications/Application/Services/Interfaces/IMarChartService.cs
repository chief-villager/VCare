using VCare.SharedKernel.Results;

namespace Medications.Application.Services.Interfaces
{
    public interface IMarChartService
    {
        Task<Result<MarChartResponse>> BuildChart(Guid patientId, int year, int month, CancellationToken token);

    }
}
