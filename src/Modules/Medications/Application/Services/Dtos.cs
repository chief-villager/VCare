using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Medications.Domain.Entities;
using Medications.Domain.Enum;

namespace Medications.Application.Services
{
   internal sealed record CreateMedicationOrderRequest
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

    internal record ScheduleRequest(string Dose, FrequencyType FType, List<TimeOnly> Times, int IntervalDays, DayOfWeekFlags DaysOfWeek, DateOnly? AnchorDate, DateOnly EffectiveFrom, DateOnly? EffectiveTo, int Sequence);
    internal record DueSlot(Guid OrderId, Guid ScheduleId, string Dose, DateTime DueAt);
}