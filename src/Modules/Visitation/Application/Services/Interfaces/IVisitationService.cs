using VCare.Modules.Visitation.Application.Services;
using VCare.SharedKernel.Results;

namespace Visitation.Application.Services.Interfaces;

public interface IVisitationService
{
    // Returns the ids of the visits created, one per requested day.
    Task<Result<IReadOnlyList<Guid>>> ScheduleVisitsAsync(Guid patientId, ScheduleVisitRequest request, CancellationToken token);
    Task<Result> AssignStaffAsync(Guid visitId, AssignStaffRequest request, CancellationToken token);

    // The staff member checking in or out is the caller.
    Task<Result> CheckInAsync(Guid visitId, CheckInVisitRequest request, CancellationToken token);
    Task<Result> CheckOutAsync(Guid visitId, CheckOutVisitRequest request, CancellationToken token);
    Task<Result> CancelVisitAsync(Guid visitId, CancelVisitRequest request, CancellationToken token);

    Task<Result<VisitResponse?>> GetByIdAsync(Guid visitId, CancellationToken token);

    // A patient's visits. No visits is an empty list, not a failure.
    Task<Result<IReadOnlyList<VisitResponse>>> GetForPatientAsync(Guid patientId, CancellationToken token);
}
