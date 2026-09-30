using Medications.Application.Services;

namespace Medications.Application.Services.Interfaces
{
    public interface IMarChartService
    {
        Task<MarChartResponse> BuildChart(Guid patientId, int year, int month);

    }
}
