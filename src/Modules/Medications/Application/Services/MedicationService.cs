using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Medications.Application.Abstracts;
using Medications.Application.Services.Interfaces;
using Medications.Domain.Entities;
using Medications.Domain.Enum;
using VCare.SharedKernel.Abstractions;
using Medications.Domain.Errors;
using VCare.SharedKernel.Results;

namespace Medications.Application.Services
{
    internal class MedicationService( IMedicationOrderRepository _medicationOrder,
    IMedicalAdministrationRepository _medicalAdministrationRepository,
    ScheduleExpander _expander, IOutcomeRepository outcomeRepository,
    ICurrentUser currentUser) : IMedicationService
    {
        public async Task<Result<CreateMedicationOrderResponse>>CreateMedicationOrderAsync( Guid patientId, CreateMedicationOrderRequest request, CancellationToken token)
        {
            var route = RouteEnumChecker.MedicationRouteChecker(request.MedicationRoute);
            if (route.IsFailure)
            {
                return Result.Failure<CreateMedicationOrderResponse>(route.Error);
            }
            var status = OrderStatusEnumCheker.OrderStatusCheker(request.Status);
            if (status.IsFailure)
            {
                return Result.Failure<CreateMedicationOrderResponse>(status.Error);
            }

            // Every schedule is checked before any is used: reading Value on a failed
            // result throws, which would turn a bad request into a 500.
            var schedules = new List<(string, FrequencyType, List<TimeOnly>, int, DayOfWeekFlags, DateOnly?, DateOnly, DateOnly?, int)>();
            foreach (var s in request.Schedules)
            {
                var frequency = FrequencyTypeEnumChecker.FrequencyTypeChecker(s.FType);
                if (frequency.IsFailure)
                {
                    return Result.Failure<CreateMedicationOrderResponse>(frequency.Error);
                }
                var days = DaysOfWeekEnumChecker.DaysOfWeekChecker(s.DaysOfWeek);
                if (days.IsFailure)
                {
                    return Result.Failure<CreateMedicationOrderResponse>(days.Error);
                }
                schedules.Add((s.Dose, frequency.Value, s.Times, s.IntervalDays, days.Value, s.AnchorDate, s.EffectiveFrom, s.EffectiveTo, s.Sequence));
            }

            var medicationOrder = MedicationOrder.Record(patientId,currentUser.CareHomeId,request.MedicatioName,route.Value,request.Instructions,
            request.StartDate,request.EndDate,request.Prescriber,request.IsPrn,request.PrnIndication,
            request.PrnMinIntervalMinutes, request.PrnMaxDose24h,
            request.IsControlledDrug, status.Value,
            schedules,
            request.CreatedAt);
            if (medicationOrder.IsFailure)
            {
                return Result.Failure<CreateMedicationOrderResponse>(medicationOrder.Error);
            }
            await _medicationOrder.AddAsync(medicationOrder.Value, token);
            await _medicationOrder.SaveChangesAsync(token);
            return Result.Success(new CreateMedicationOrderResponse(medicationOrder.Value.Id.Value, medicationOrder.Value.Medication));
        }

        public async Task<Result>UpdateMedicationOrderStatusAsync(Guid medicationOrderId, string status, CancellationToken token)
        {
            var medicationOrder = await _medicationOrder.GetAsync(medicationOrderId, token);
            if (medicationOrder is null)
            {
                return Result.Failure(MedicationErrors.OrderNotFound);
            }
            if (medicationOrder.Status != OrderStatus.Active)
            {
                return Result.Failure(Error.Conflict("Medications.OrderNotActive", "Cannot update status of a completed or cancelled order."));
            }
            var statusResult = medicationOrder.UpdateStatus(status);
            if (statusResult.IsFailure)
            {
                return statusResult;
            }
            await _medicationOrder.SaveChangesAsync(token);
            return Result.Success();
        }

        public async Task<Result<IEnumerable<DueSlot>>> GetDueForDay(Guid patientId, DateOnly day, CancellationToken token)
        {
            var orders = await _medicationOrder.ActiveBetween(patientId, day, day);
            var slots = orders.SelectMany(order => _expander.ExpandSchedule(order, day, day));
            return Result.Success(slots);
        }

        // Slots still needing action today: the day's scheduled doses minus the ones
        // already administered. A slot counts as done once any administration exists
        // for its (order, due time) — Given, Refused and Omitted all sign the slot off.
        public async Task<Result<IEnumerable<DueSlot>>> GetOutstandingForDay(Guid patientId, DateOnly day, CancellationToken token)
        {
            var orders = await _medicationOrder.ActiveBetween(patientId, day, day);
            var slots = orders.SelectMany(order => _expander.ExpandSchedule(order, day, day));

            var signed = _medicalAdministrationRepository
                .ForResidentBetween(patientId, day, day)
                .Where(a => a.ScheduledFor.HasValue)
                .Select(a => (a.MedicationOrderId.Value, a.ScheduledFor!.Value))
                .ToHashSet();

            var outstanding = slots.Where(slot => !signed.Contains((slot.OrderId, slot.DueAt)));
            return Result.Success(outstanding);
        }



        public async Task<Result<MedicationAdministrationResponse>> RecordAministration(Guid orderId,DateTime scheduledFor, string outcome,        
        Guid staffId, Guid scheduleId, CancellationToken token, string? notes = null)
        {
            var order = await _medicationOrder.GetAsync(orderId, token);
            if (order is null)
            {
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.OrderNotFound);
            }
            if (order.Status != OrderStatus.Active)
            {
                return Result.Failure<MedicationAdministrationResponse>(Error.Conflict("Medications.OrderNotActive", "Order is not active."));
            }
            var day = DateOnly.FromDateTime(scheduledFor);
            var isSlot = _expander.ExpandSchedule(order,day,day).Any( s => s.ScheduleId == scheduleId && s.DueAt == scheduledFor);
            if (!isSlot)
            {
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.NotAScheduledSlot);
            }
            if (await _medicalAdministrationRepository.Find(orderId, scheduledFor) is not null)
            {
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.DoseAlreadyRecorded);
            }
            var now = DateTime.Now;
            var late = now > scheduledFor.AddHours(1);

            // A late dose is signed off as missed rather than with the caller's outcome:
            // nothing was administered, so there is no administration time, and the
            // caller is told via a failure that their outcome was not recorded.
            var outcomeName = late ? "missed" : outcome;
            var outcomeResult = await outcomeRepository.GetOutcomeByName(outcomeName, token);
            if (outcomeResult == null)
            {
                return Result.Failure<MedicationAdministrationResponse>("Outcome not Found");
            }
            var administration = MedicationAdministration.Create(orderId, order.PatientId.Value, order.CareHomeId.Value, scheduledFor,
                late ? null : now, outcomeResult.Id, staffId,
                late ? "Did not make it in time to administer medication" : notes);
            if (administration.IsFailure)
            {
                return Result.Failure<MedicationAdministrationResponse>(administration.Error);
            }
            await _medicalAdministrationRepository.AddAsync(administration.Value);
            await _medicalAdministrationRepository.SaveChangesAsync(token);
            if (late)
            {
                return Result.Failure<MedicationAdministrationResponse>(MedicationErrors.AdministrationWindowElapsed);
            }
            return Result.Success(new MedicationAdministrationResponse(
                administration.Value.Id.Value,
                administration.Value.MedicationOrderId.Value,
                administration.Value.ScheduledFor,
                administration.Value.AdministeredAt,
                administration.Value.OutcomeCodeId,
                administration.Value.AdministeredByStaffId,
                administration.Value.Notes
            ));
            
        }

        public async Task<Result<MedicationOrderResponse>> GetMedicationOrder(Guid orderId, CancellationToken token)
        {
            var order = await _medicationOrder.GetAsync(orderId, token);
            if (order is null)
            {
                return Result.Failure<MedicationOrderResponse>(MedicationErrors.OrderNotFound);
            }
            return Result.Success(new MedicationOrderResponse(
                order.Id.Value,
                order.Medication,
                order.Route,
                order.Instructions,
                order.StartDate,
                order.EndDate,
                order.Prescriber,
                order.IsPrn,
                order.PrnIndication,
                order.PrnMinIntervalMinutes,
                order.PrnMaxDose24h,
                order.IsControlledDrug,
                order.Schedule.Select(s => new ScheduleResponse(
                    s.Id,
                    s.Dose,
                    s.FType,
                    [.. s.Times],
                    s.IntervalDays,
                    s.DaysOfWeek,
                    s.AnchorDate,
                    s.EffectiveFrom,
                    s.EffectiveTo,
                    s.Sequence
                )).ToList(),
                order.Status,
                order.CreatedAt
            ));
        }
    }
}