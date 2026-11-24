using VCare.Modules.Patients.Application.Abstractions;
using VCare.Modules.Patients.Application.Dtos;
using VCare.Modules.Patients.Domain.Entities;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace VCare.Modules.Patients.Application.Services;

/// <summary>
/// Allergies and absences: the parts of the resident's record that other
/// modules act on. Medications refuses an order that matches an allergy, and does
/// not count a dose as missed while the resident was in hospital or on leave.
/// The staff member and the time come from the caller and the home's clock.
/// </summary>
internal sealed class ResidentRecordService(IPatientRepository repository, ICurrentUser currentUser, ICareHomeClock homeClock)
{
    // Lets a carer's device clock run a little ahead without refusing the entry.
    private static readonly TimeSpan ClockTolerance = TimeSpan.FromMinutes(5);

    private static readonly Error PatientNotFound = Error.NotFound("Patients.NotFound", "Resident not found.");
    private static readonly Error NhsNumberInUse = Error.Conflict("Patients.NhsNumberInUse", "Another resident in this home has that NHS number.");

    public async Task<Result<AllergiesResponse>> GetAllergiesAsync(Guid patientId, CancellationToken token)
    {
        var patient = await repository.GetByIdAsync(patientId, token);
        if (patient is null)
            return Result.Failure<AllergiesResponse>(PatientNotFound);
        return Result.Success(new AllergiesResponse(patient.AllergyStatus,
            [.. patient.Allergies.OrderBy(a => a.RecordedAt).Select(a => new AllergyResponse(
                a.Id, a.Substance, a.Reaction, a.Severity, a.RecordedAt, a.RecordedByStaffId, a.RemovedAt, a.RemovalReason))]));
    }

    public async Task<Result<Guid>> RecordAllergyAsync(Guid patientId, RecordAllergyRequest request, CancellationToken token)
    {
        var severity = AllergySeverity.Unknown;
        if (!string.IsNullOrWhiteSpace(request.Severity)
            && (!Enum.TryParse(request.Severity.Trim(), ignoreCase: true, out severity) || !Enum.IsDefined(severity)))
            return Result.Failure<Guid>(Error.Validation("Patients.InvalidSeverity",
                $"Severity must be one of: {string.Join(", ", Enum.GetNames<AllergySeverity>())}."));

        var patient = await repository.GetByIdAsync(patientId, token);
        if (patient is null)
            return Result.Failure<Guid>(PatientNotFound);

        var allergy = patient.RecordAllergy(request.Substance, request.Reaction, severity, currentUser.UserId, await NowAsync(token));
        if (allergy.IsFailure)
            return Result.Failure<Guid>(allergy.Error);
        await repository.SaveChangesAsync(token);
        return Result.Success(allergy.Value.Id);
    }

    public Task<Result> RecordNoKnownAllergiesAsync(Guid patientId, CancellationToken token) =>
        MutateAsync(patientId, (patient, now) => patient.RecordNoKnownAllergies(currentUser.UserId, now), token);

    public Task<Result> RemoveAllergyAsync(Guid patientId, Guid allergyId, RemoveAllergyRequest request, CancellationToken token) =>
        MutateAsync(patientId, (patient, now) => patient.RemoveAllergy(allergyId, request.Reason, currentUser.UserId, now), token);

    public async Task<Result<IReadOnlyList<AbsenceResponse>>> GetAbsencesAsync(Guid patientId, CancellationToken token)
    {
        var patient = await repository.GetByIdAsync(patientId, token);
        if (patient is null)
            return Result.Failure<IReadOnlyList<AbsenceResponse>>(PatientNotFound);
        return Result.Success<IReadOnlyList<AbsenceResponse>>(
            [.. patient.Absences.OrderBy(a => a.StartedAt).Select(a => new AbsenceResponse(
                a.Id, a.Kind, a.StartedAt, a.EndedAt, a.Note, a.RecordedByStaffId, a.EndedByStaffId))]);
    }

    public async Task<Result<Guid>> StartAbsenceAsync(Guid patientId, StartAbsenceRequest request, CancellationToken token)
    {
        // Discharge and death have their own endpoints: they end the residency.
        if (string.IsNullOrWhiteSpace(request.Kind)
            || !Enum.TryParse<AbsenceKind>(request.Kind.Trim(), ignoreCase: true, out var kind)
            || kind is not (AbsenceKind.Hospital or AbsenceKind.Leave))
            return Result.Failure<Guid>(Error.Validation("Patients.InvalidAbsenceKind", "Kind must be Hospital or Leave."));

        var patient = await repository.GetByIdAsync(patientId, token);
        if (patient is null)
            return Result.Failure<Guid>(PatientNotFound);

        var now = await NowAsync(token);
        var startedAt = request.StartedAt ?? now;
        if (startedAt > now + ClockTolerance)
            return Result.Failure<Guid>(Error.Validation("Patients.AbsenceInFuture",
                "An absence is recorded when it happens, not in advance."));

        var absence = patient.StartAbsence(kind, startedAt, request.Note, currentUser.UserId);
        if (absence.IsFailure)
            return Result.Failure<Guid>(absence.Error);
        await repository.SaveChangesAsync(token);
        return Result.Success(absence.Value.Id);
    }

    public async Task<Result> EndAbsenceAsync(Guid patientId, EndAbsenceRequest request, CancellationToken token)
    {
        var now = await NowAsync(token);
        var endedAt = request.EndedAt ?? now;
        if (endedAt > now + ClockTolerance)
            return Result.Failure(Error.Validation("Patients.AbsenceInFuture", "A return is recorded when it happens, not in advance."));
        return await MutateAsync(patientId, (patient, _) => patient.EndAbsence(endedAt, currentUser.UserId), token);
    }

    public async Task<Result> UpdateCareDetailsAsync(Guid patientId, UpdateCareDetailsRequest request, CancellationToken token)
    {
        var patient = await repository.GetByIdAsync(patientId, token);
        if (patient is null)
            return Result.Failure(PatientNotFound);
        var updated = patient.UpdateCareDetails(request.NhsNumber, request.GpName, request.GpPractice, request.GpPhone,
            request.Room, request.AdmittedOn, await NowAsync(token));
        if (updated.IsFailure)
            return updated;
        // Two records with one NHS number is how a resident's medicines end up on the wrong chart.
        if (patient.NhsNumber is { } nhs && await repository.NhsNumberInUseAsync(nhs, patientId, token))
            return Result.Failure(NhsNumberInUse);
        try
        {
            await repository.SaveChangesAsync(token);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException) when (patient.NhsNumber is not null)
        {
            // Another resident was given the number between the check and this save,
            // and the database's unique index refused it. Any other failure is still an error.
            if (await repository.NhsNumberInUseAsync(patient.NhsNumber, patientId, CancellationToken.None))
                return Result.Failure(NhsNumberInUse);
            throw;
        }
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<WeightResponse>>> GetWeightsAsync(Guid patientId, CancellationToken token)
    {
        var patient = await repository.GetByIdAsync(patientId, token);
        if (patient is null)
            return Result.Failure<IReadOnlyList<WeightResponse>>(PatientNotFound);
        return Result.Success<IReadOnlyList<WeightResponse>>(
            [.. patient.Weights.OrderBy(w => w.MeasuredAt).Select(w => new WeightResponse(w.Id, w.Kilograms, w.MeasuredAt, w.RecordedByStaffId))]);
    }

    public async Task<Result<Guid>> RecordWeightAsync(Guid patientId, RecordWeightRequest request, CancellationToken token)
    {
        var patient = await repository.GetByIdAsync(patientId, token);
        if (patient is null)
            return Result.Failure<Guid>(PatientNotFound);
        var now = await NowAsync(token);
        var measuredAt = request.MeasuredAt ?? now;
        if (measuredAt > now + ClockTolerance)
            return Result.Failure<Guid>(InFuture("A weight"));
        var weight = patient.RecordWeight(request.Kilograms, measuredAt, currentUser.UserId, now);
        if (weight.IsFailure)
            return Result.Failure<Guid>(weight.Error);
        await repository.SaveChangesAsync(token);
        return Result.Success(weight.Value.Id);
    }

    public async Task<Result> DischargeAsync(Guid patientId, DischargeRequest request, CancellationToken token)
    {
        var now = await NowAsync(token);
        var at = request.At ?? now;
        if (at > now + ClockTolerance)
            return Result.Failure(InFuture("A discharge"));
        return await MutateAsync(patientId, (patient, _) => patient.Discharge(at, request.Destination, currentUser.UserId), token);
    }

    public async Task<Result> RecordDeathAsync(Guid patientId, RecordDeathRequest request, CancellationToken token)
    {
        var now = await NowAsync(token);
        var at = request.At ?? now;
        if (at > now + ClockTolerance)
            return Result.Failure(InFuture("A death"));
        return await MutateAsync(patientId, (patient, _) => patient.RecordDeath(at, currentUser.UserId), token);
    }

    public async Task<Result> ReadmitAsync(Guid patientId, ReadmitRequest request, CancellationToken token)
    {
        var now = await NowAsync(token);
        var at = request.At ?? now;
        if (at > now + ClockTolerance)
            return Result.Failure(InFuture("A readmission"));
        return await MutateAsync(patientId, (patient, _) => patient.Readmit(at, currentUser.UserId, now), token);
    }

    public async Task<Result<IReadOnlyList<AttorneyResponse>>> GetAttorneysAsync(Guid patientId, CancellationToken token)
    {
        var patient = await repository.GetByIdAsync(patientId, token);
        if (patient is null)
            return Result.Failure<IReadOnlyList<AttorneyResponse>>(PatientNotFound);
        return Result.Success<IReadOnlyList<AttorneyResponse>>(
            [.. patient.Attorneys.OrderBy(a => a.RecordedAt).Select(a => new AttorneyResponse(a.Id, a.Name, a.Kind, a.Phone, a.Email,
                a.RegisteredOn, a.RecordedAt, a.RecordedByStaffId, a.RemovedAt, a.RemovalReason))]);
    }

    public async Task<Result<Guid>> RecordAttorneyAsync(Guid patientId, RecordAttorneyRequest request, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(request.Kind)
            || !Enum.TryParse<AttorneyKind>(request.Kind.Trim(), ignoreCase: true, out var kind) || !Enum.IsDefined(kind))
            return Result.Failure<Guid>(Error.Validation("Patients.InvalidAttorneyKind",
                "Kind must be HealthAndWelfare or PropertyAndFinancialAffairs."));
        var patient = await repository.GetByIdAsync(patientId, token);
        if (patient is null)
            return Result.Failure<Guid>(PatientNotFound);
        var attorney = patient.RecordAttorney(request.Name, kind, request.Phone, request.Email, request.RegisteredOn,
            currentUser.UserId, await NowAsync(token));
        if (attorney.IsFailure)
            return Result.Failure<Guid>(attorney.Error);
        await repository.SaveChangesAsync(token);
        return Result.Success(attorney.Value.Id);
    }

    public Task<Result> RemoveAttorneyAsync(Guid patientId, Guid attorneyId, RemoveAttorneyRequest request, CancellationToken token) =>
        MutateAsync(patientId, (patient, now) => patient.RemoveAttorney(attorneyId, request.Reason, currentUser.UserId, now), token);

    private static Error InFuture(string what) =>
        Error.Validation("Patients.InFuture", $"{what} is recorded when it happens, not in advance.");

    private Task<DateTime> NowAsync(CancellationToken token) => homeClock.NowAsync(currentUser.CareHomeId, token);

    private async Task<Result> MutateAsync(Guid patientId, Func<Patient, DateTime, Result> change, CancellationToken token)
    {
        var patient = await repository.GetByIdAsync(patientId, token);
        if (patient is null)
            return Result.Failure(PatientNotFound);
        var result = change(patient, await NowAsync(token));
        if (result.IsFailure)
            return result;
        await repository.SaveChangesAsync(token);
        return Result.Success();
    }
}
