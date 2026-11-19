using Medications.Domain.Enum;

namespace Medications.Application.Services
{
   // CreatedAt is no longer taken from the client: when an order entered VCare is
   // what decides which earlier doses the system can call missed, so the server
   // stamps it. An old client still sending it is ignored, not rejected.
   public sealed record CreateMedicationOrderRequest
   (    string MedicatioName,
        string MedicationRoute,
        string? Instructions,
        DateOnly StartDate,
        DateOnly? EndDate,
        string Prescriber,
        bool IsPrn,
        string? PrnIndication,
        int? PrnMinIntervalMinutes,
        string? PrnMaxDose24h,
        bool IsControlledDrug,
        List<ScheduleRequest> Schedules,
        string Status,
        // The number of PRN doses allowed in any 24 hours; enforced at sign-off.
        int? PrnMaxDosesPer24h = null
    );

    public record ScheduleRequest(string Dose, string FType, List<TimeOnly> Times, int IntervalDays, string? DaysOfWeek, DateOnly? AnchorDate, DateOnly EffectiveFrom, DateOnly? EffectiveTo, int Sequence);
    public record DueSlot(Guid OrderId,string MedicationName,Guid ScheduleId, string Dose, DateTime DueAt);

    public sealed record CreateMedicationOrderResponse
    (
        Guid OrderId,
        string MedicationName
    );

    public sealed record MedicationOrderResponse
    (
        Guid OrderId,
        string MedicationName,
        MedicationRouteEnum Route,
        string? Instructions,
        DateOnly StartDate,
        DateOnly? EndDate,
        string Prescriber,
        bool IsPrn,
        string? PrnIndication,
        int? PrnMinIntervalMinutes,
        string? PrnMaxDose24h,
        bool IsControlledDrug,
        List<ScheduleResponse> Schedules,
        OrderStatus Status,
        DateTime CreatedAt,
        int? PrnMaxDosesPer24h = null
    );

    public record ScheduleResponse(Guid ScheduleId,string Dose, FrequencyType FType, List<TimeOnly> Times, int IntervalDays, DayOfWeekFlags DaysOfWeek, DateOnly? AnchorDate, DateOnly EffectiveFrom, DateOnly? EffectiveTo, int Sequence);

    /// <summary>
    /// Signing off one scheduled dose. <see cref="AdministeredAt"/> is when it was
    /// actually given, for a carer signing a few minutes after the round; it
    /// defaults to now. <see cref="Reason"/> is required once the on-time window
    /// has closed: why the dose was late.
    /// </summary>
    public sealed record RecordAdministrationRequest
    (
        DateTime ScheduledFor,
        string Outcome,
        string? Notes,
        DateTime? AdministeredAt = null,
        string? Reason = null
    );

    /// <summary>
    /// Recording one as-required dose. PRN doses have no slot, so the dose given
    /// and the reason for giving it are both required.
    /// </summary>
    public sealed record RecordPrnAdministrationRequest
    (
        string Outcome,
        string DoseGiven,
        string Reason,
        DateTime? AdministeredAt = null,
        string? Notes = null
    );

    public sealed record UpdateMedicationOrderStatusRequest(string Status);

    public sealed record MedicationAdministrationResponse
    (
        Guid Id,
        Guid OrderId,
        DateTime? ScheduledFor,
        DateTime? AdministeredAt,
        Guid OutcomeCodeId,
        Guid AdministeredByStaffId,
        string? Notes,
        bool IsLate = false,
        string? Reason = null,
        string? DoseGiven = null
    );


}
