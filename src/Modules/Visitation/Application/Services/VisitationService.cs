using VCare.Modules.Visitation.Application.Abstractions;
using VCare.Modules.Visitation.Domain.Entities;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;
using Visitation.Application.Services.Interfaces;
using Visitation.Domain.Entities;

namespace VCare.Modules.Visitation.Application.Services;

internal sealed class VisitationService(IVisitRepository visitRepository, ICurrentUser currentUser,
    IPatientDirectory patients, IStaffDirectory staff) : IVisitationService
{
    private static readonly Error VisitNotFound = Error.NotFound("Visits.NotFound", "Visit not found.");

    public async Task<Result<IReadOnlyList<Guid>>> ScheduleVisitsAsync(Guid patientId, ScheduleVisitRequest request, CancellationToken token)
    {
        // The patient comes from the URL and Patients owns it: without this a visit
        // could be booked for a mistyped id or another home's resident.
        if (!await patients.ExistsAsync(patientId, token))
            return Result.Failure<IReadOnlyList<Guid>>(Error.NotFound("Visits.PatientNotFound", "Resident not found."));

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

    public async Task<Result> AssignStaffAsync(Guid patientId, Guid visitId, AssignStaffRequest request, CancellationToken token)
    {
        // An id that is nobody in this home could never check in, so the visit
        // would silently never happen.
        if (!await staff.ExistsAsync(request.StaffId, token))
            return Result.Failure(Error.NotFound("Visits.StaffNotFound", "Staff member not found."));

        return await MutateAsync(patientId, visitId, visit => visit.AssignVisit(new StaffId(request.StaffId)), token);
    }

    public Task<Result> CheckInAsync(Guid patientId, Guid visitId, CheckInVisitRequest request, CancellationToken token) =>
        MutateAsync(patientId, visitId, visit => visit.Checkin(request.CheckedInAt, new StaffId(currentUser.UserId)), token);

    public Task<Result> CheckOutAsync(Guid patientId, Guid visitId, CheckOutVisitRequest request, CancellationToken token) =>
        MutateAsync(patientId, visitId, visit => visit.CheckOut(
            new StaffId(currentUser.UserId),
            request.IsMedicationCompleted, request.IsPersonalCareCompleted,
            request.IsFeedingTaskCompleted, request.MedicationTaskNote,
            request.PersonalCareTaskNote, request.FeedingTaskNote,
            request.CheckedOutAt), token);

    public Task<Result> CancelVisitAsync(Guid patientId, Guid visitId, CancelVisitRequest request, CancellationToken token) =>
        MutateAsync(patientId, visitId, visit => visit.Cancel(request.Reason), token);

    public async Task<Result<VisitResponse?>> GetByIdAsync(Guid patientId, Guid visitId, CancellationToken token)
    {
        var visit = await GetForPatientAsync(patientId, visitId, token);
        return Result.Success(visit is null ? null : ToResponse(visit));
    }

    public async Task<Result<IReadOnlyList<VisitResponse>>> GetForPatientAsync(Guid patientId, CancellationToken token)
    {
        var visits = await visitRepository.GetForPatientAsync(new PatientId(patientId), token);
        return Result.Success<IReadOnlyList<VisitResponse>>([.. visits.Select(ToResponse)]);
    }

    // Load, apply one domain transition, save. Every state change on a visit is
    // the same three steps; only the transition differs.
    private async Task<Result> MutateAsync(Guid patientId, Guid visitId, Func<Visit, Result> transition, CancellationToken token)
    {
        var visit = await GetForPatientAsync(patientId, visitId, token);
        if (visit is null)
            return Result.Failure(VisitNotFound);

        var result = transition(visit);
        if (result.IsFailure)
            return result;

        await visitRepository.SaveChangesAsync(token);
        return Result.Success();
    }

    // A visit reached through another resident's URL is treated as missing: the
    // patient in the route and the visit's own patient must agree.
    private async Task<Visit?> GetForPatientAsync(Guid patientId, Guid visitId, CancellationToken token)
    {
        var visit = await visitRepository.GetByIdAsync(new VisitId(visitId), token);
        return visit is not null && visit.PatientId.Value == patientId ? visit : null;
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
