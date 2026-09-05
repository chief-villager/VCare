using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Medications.Domain.Entities;
using Medications.Domain.Enum;

namespace Medications.Application.Services
{
   public sealed record CreateMedicationOrderRequest
   (    string MedicatioName, 
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
        List<ScheduleRequest> Schedules,
        OrderStatus Status,
        DateTime CreatedAt
    );

    public record ScheduleRequest(string Dose, FrequencyType FType, List<TimeOnly> Times, int IntervalDays, DayOfWeekFlags DaysOfWeek, DateOnly? AnchorDate, DateOnly EffectiveFrom, DateOnly? EffectiveTo, int Sequence);
    public record DueSlot(Guid OrderId, Guid ScheduleId, string Dose, DateTime DueAt);

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
        DateTime CreatedAt
    );

    public record ScheduleResponse(string Dose, FrequencyType FType, List<TimeOnly> Times, int IntervalDays, DayOfWeekFlags DaysOfWeek, DateOnly? AnchorDate, DateOnly EffectiveFrom, DateOnly? EffectiveTo, int Sequence);

    public sealed record MedicationAdministrationResponse
    (
        Guid Id,
        Guid OrderId,
        DateTime? ScheduledFor,
        DateTime? AdministeredAt,
        Guid OutcomeCodeId,
        Guid AdministeredByStaffId,
        Guid? WitnessedByStaffId,
        string? Notes
    );
}