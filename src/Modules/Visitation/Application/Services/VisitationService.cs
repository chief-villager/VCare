using VCare.Modules.Visitation.Application.Abstractions;
using VCare.Modules.Visitation.Domain.Entities;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;
using Visitation.Application.Services.Interfaces;

namespace VCare.Modules.Visitation.Application.Services;

internal sealed class VisitationService(IVisitRepository visitRepository, ICurrentUser currentUser) : IVisitationService
{
    public async Task<Result> CheckInAsync(Guid patientId, CheckInVisitRequest request, CancellationToken token)
    {
        var visit = Visit.CreateCheckin(patientId, currentUser.CareHomeId, currentUser.UserId, request.CheckedInAt);
        if (visit.IsFailure)
        {
            return Result.Failure(visit.Error);
        }
        await visitRepository.AddAsync(visit.Value, token);
        await visitRepository.SaveChangesAsync(token);
        return Result.Success();
    }
        
    public Task<Result> CheckOutAsync(Guid visitId, CheckOutVisitRequest request, CancellationToken token) =>
        MutateAsync(visitId, visit => visit.CheckOut(
            request.IsMedicationCompleted, request.IsPersonalCareCompleted,
            request.IsFeedingTaskCompleted, request.MedicationTaskNote,
            request.PersonalCareTaskNote, request.FeedingTaskNote), token);

    public Task<Result> CancelVisitAsync(Guid visitId, CancelVisitRequest request, CancellationToken token) =>
        MutateAsync(visitId, visit => visit.Cancel(request.Reason), token);

    public async Task<Result<VisitResponse?>> GetByIdAsync(Guid visitId, CancellationToken token)
    {
        var visit = await visitRepository.GetByIdAsync(new VisitId(visitId), token);
        return Result.Success(visit is null ? null : ToResponse(visit));
    }

    public async Task<Result<IReadOnlyList<VisitResponse>>> GetForPatientAsync(Guid patientId, CancellationToken token)
    {
        var visits = await visitRepository.GetForPatientAsync(new PatientId(patientId), token);
        return Result.Success<IReadOnlyList<VisitResponse>>([.. visits.Select(ToResponse)]);
    }

    // Load, apply one domain transition, save. Every state change on a visit is
    // the same three steps; only the transition differs.
    private async Task<Result> MutateAsync(Guid visitId, Func<Visit, Result> transition, CancellationToken token)
    {
        var visit = await visitRepository.GetByIdAsync(new VisitId(visitId), token);
        if (visit is null)
            return Result.Failure("Visit not found.");

        var result = transition(visit);
        if (result.IsFailure)
            return result;

        await visitRepository.SaveChangesAsync(token);
        return Result.Success();
    }

    // The three task records only exist once the visit is checked out, so a
    // visit still in progress reports empty lists rather than blowing up.
    private static VisitResponse ToResponse(Visit visit) =>
        new(visit.Id.Value, visit.PatientId.Value, visit.StaffId.Value,
            visit.Status.ToString(),
            visit.FeedingTask is null
                ? []
                : [new FeedingTask(visit.FeedingTask.FeedingTaskNote, visit.FeedingTask.IsFeedingCompleted)],
            visit.MedicationTask is null
                ? []
                : [new MedicationTask(visit.MedicationTask.MedicationTaskNote, visit.MedicationTask.IsMedicationCompleted)],
            visit.PersonalcareTask is null
                ? []
                : [new PersonalCareTask(visit.PersonalcareTask.CareTaskNote, visit.PersonalcareTask.IsCareCompleted)],
            visit.CheckedInAt, visit.CheckedOutAt);

}
