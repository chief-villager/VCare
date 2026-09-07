using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Medications.Application.Abstracts;
using Medications.Application.Services.Interfaces;
using Medications.Domain.Entities;
using Medications.Domain.Enum;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace Medications.Application.Services
{
    internal class MedicationService( IMedicationOrderRepository _medicationOrder,
    IMedicalAdministrationRepository _medicalAdministrationRepository,
    ScheduleExpander _expander,
    IUnitOfWork unitOfWork) : IMedicationService
    {
        public async Task<Result<CreateMedicationOrderResponse>>CreateMedicationOrderAsync( Guid patientId, CreateMedicationOrderRequest request, CancellationToken token)
        {
            var medicationOrder = MedicationOrder.Record(patientId,request.MedicatioName,request.Route,request.Instructions,
            request.StartDate,request.EndDate,request.Prescriber,request.IsPrn,request.PrnIndication,
            request.IsControlledDrug, request.Status,
            request.Schedules.Select(s => (s.Dose, s.FType, s.Times, s.IntervalDays, s.DaysOfWeek, s.AnchorDate, s.EffectiveFrom, s.EffectiveTo, s.Sequence)), 
            request.CreatedAt);
            if (medicationOrder.IsFailure)
            {
                return Result.Failure<CreateMedicationOrderResponse>(medicationOrder.Error);
            }
            await _medicationOrder.AddAsync(medicationOrder.Value, token);
            await unitOfWork.SaveChangesAsync(token);
            return Result.Success(new CreateMedicationOrderResponse(medicationOrder.Value.Id.Value, medicationOrder.Value.Medication));
        }

        public async Task<Result>UpdateMedicationOrderStatusAsync(Guid medicationOrderId, string status, CancellationToken token)
        {
            var medicationOrder = await _medicationOrder.GetAsync(medicationOrderId, token);
            if (medicationOrder.Status != OrderStatus.Active)
            {
                return Result.Failure("Cannot update status of completed or cancelled order");
            }
            medicationOrder.UpdateStatus(status);
            await unitOfWork.SaveChangesAsync(token);
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



        public async Task<Result<MedicationAdministrationResponse>> RecordAministration(Guid orderId,DateTime scheduledFor, Guid outcomeCodeId,        
        Guid staffId, CancellationToken token, string? notes = null)
        {
            

            var order = await  _medicationOrder.GetAsync(orderId, token) ?? throw new InvalidOperationException("Order not found");
            if (order.Status != OrderStatus.Active)
            {
                throw new InvalidOperationException("Order is not active.");
            }
            if (_medicalAdministrationRepository.Find(orderId, scheduledFor) is not null)
            {
                throw new InvalidOperationException("This dose is already recorded.");
            }
            var administration = MedicationAdministration.Create(orderId, order.PatientId.Value, scheduledFor,DateTime.Now, outcomeCodeId, staffId,notes);
            if (administration.IsFailure)
            {
                return Result.Failure<MedicationAdministrationResponse>(administration.Error);
            }
            await _medicalAdministrationRepository.AddAsync(administration.Value);
            await unitOfWork.SaveChangesAsync(token);
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