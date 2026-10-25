using VCare.Modules.Visitation.Application.Abstractions;
using VCare.Modules.Visitation.Domain.Entities;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;
using Visitation.Application.Services.Interfaces;
using Visitation.Domain.Entities;

namespace VCare.Modules.Visitation.Application.Services;

internal sealed class VisitationService(IVisitRepository visitRepository, ICurrentUser currentUser) : IVisitationService
{
    public async Task<Result<IReadOnlyList<Guid>>> ScheduleVisitsAsync(Guid patientId, ScheduleVisitRequest request, CancellationToken token)
    {
        var visits = Visit.CreateVisit(new PatientId(patientId), new CareHomeId(currentUser.CareHomeId),
            request.VisitDays, request.VisitTime);
        if (visits.IsFailure)
        {
            return Result.Failure<IReadOnlyList<Guid>>(visits.Error);
        }
        await visitRepository.AddRangeAsync(visits.Value, token);
        await visitRepository.SaveChangesAsync(token);
        return Result.Success<IReadOnlyList<Guid>>([.. visits.Value.Select(v => v.Id.Value)]);
    }

    public Task<Result> AssignStaffAsync(Guid visitId, AssignStaffRequest request, CancellationToken token) =>
        MutateAsync(visitId, visit => visit.AssignVisit(new StaffId(request.StaffId)), token);

    public Task<Result> CheckInAsync(Guid visitId, CheckInVisitRequest request, CancellationToken token) =>
        MutateAsync(visitId, visit => visit.Checkin(request.CheckedInAt, new StaffId(currentUser.UserId)), token);

    public Task<Result> CheckOutAsync(Guid visitId, CheckOutVisitRequest request, CancellationToken token) =>
        MutateAsync(visitId, visit => visit.CheckOut(
            new StaffId(currentUser.UserId),
            request.IsMedicationCompleted, request.IsPersonalCareCompleted,
            request.IsFeedingTaskCompleted, request.MedicationTaskNote,
            request.PersonalCareTaskNote, request.FeedingTaskNote,
            request.CheckedOutAt), token);

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
            return Result.Failure(Error.NotFound("Visits.NotFound", "Visit not found."));

        var result = transition(visit);
        if (result.IsFailure)
            return result;

        await visitRepository.SaveChangesAsync(token);
        return Result.Success();
    }

    private static VisitResponse ToResponse(Visit visit) =>
        new(visit.Id.Value, visit.PatientId.Value, visit.VisitDay, visit.VisitTime,
            visit.IsCancelled, visit.CancellationReason,
            [.. visit.StaffIds.Select(s => s.Value)],
            [.. visit.VisitRecords.Select(ToResponse)]);

    // The three task records only exist once the record is checked out, so a
    // visit still in progress reports empty lists rather than blowing up.
    private static VisitRecordResponse ToResponse(VisitRecord record) =>
        new(record.Id, record.StaffId.Value,
            record.Status.ToString(),
            record.FeedingTask is null
                ? []
                : [new FeedingTask(record.FeedingTask.FeedingTaskNote, record.FeedingTask.IsFeedingCompleted)],
            record.MedicationTask is null
                ? []
                : [new MedicationTask(record.MedicationTask.MedicationTaskNote, record.MedicationTask.IsMedicationCompleted)],
            record.PersonalcareTask is null
                ? []
                : [new PersonalCareTask(record.PersonalcareTask.CareTaskNote, record.PersonalcareTask.IsCareCompleted)],
            record.CheckedInAt, record.CheckedOutAt);
}
