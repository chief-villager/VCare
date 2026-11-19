using VCare.Modules.Visitation.Application.Services;
using VCare.SharedKernel.Results;

namespace Visitation.Application.Services.Interfaces;

// Every visit-level call takes the patient from the route as well as the visit:
// a visit reached through another resident's URL is treated as not found.
public interface IVisitationService
{
    // Returns the ids of the visits created, one per requested day.
    Task<Result<IReadOnlyList<Guid>>> ScheduleVisitsAsync(Guid patientId, ScheduleVisitRequest request, CancellationToken token);
    Task<Result> AssignStaffAsync(Guid patientId, Guid visitId, AssignStaffRequest request, CancellationToken token);

    // The staff member checking in or out is the caller.
    Task<Result> CheckInAsync(Guid patientId, Guid visitId, CheckInVisitRequest request, CancellationToken token);
    Task<Result> CheckOutAsync(Guid patientId, Guid visitId, CheckOutVisitRequest request, CancellationToken token);
    Task<Result> CancelVisitAsync(Guid patientId, Guid visitId, CancelVisitRequest request, CancellationToken token);

    Task<Result<VisitResponse?>> GetByIdAsync(Guid patientId, Guid visitId, CancellationToken token);

    // A patient's visits. No visits is an empty list, not a failure.
    Task<Result<IReadOnlyList<VisitResponse>>> GetForPatientAsync(Guid patientId, CancellationToken token);
}
