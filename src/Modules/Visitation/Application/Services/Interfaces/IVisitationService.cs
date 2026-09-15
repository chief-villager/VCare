using VCare.Modules.Visitation.Application.Services;
using VCare.SharedKernel.Results;

namespace Visitation.Application.Services.Interfaces;

public interface IVisitationService
{
   
    Task<Result> CheckInAsync(Guid patientId, CheckInVisitRequest request, CancellationToken token);
    Task<Result> CheckOutAsync(Guid visitId, CheckOutVisitRequest request, CancellationToken token);
    Task<Result> CancelVisitAsync(Guid visitId, CancelVisitRequest request, CancellationToken token);

    Task<Result<VisitResponse?>> GetByIdAsync(Guid visitId, CancellationToken token);

    // A patient's visit history. No visits is an empty list, not a failure.
    Task<Result<IReadOnlyList<VisitResponse>>> GetForPatientAsync(Guid patientId, CancellationToken token);

    // The day's visit register for the caller's care home.
}
