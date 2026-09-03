using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Medications.Application.Abstracts;
using Medications.Domain.Entities;
using Medications.Domain.Enum;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace Medications.Application.Services
{
    internal class MedicationService( IMedicationOrderRepository _medicationOrder, 
    IMedicalAdministrationRepository _medicalAdministrationRepository, IUnitOfWork unitOfWork)
    {
        public async Task<Result<MedicationOrder>>CreateMedicationOrderAsync( Guid patientId, CreateMedicationOrderRequest request, CancellationToken token)
        {
            var medicationOrder = MedicationOrder.Record(patientId,request.MedicatioName,request.Route,request.Instructions,
            request.StartDate,request.EndDate,request.Prescriber,request.IsPrn,request.PrnIndication,
            request.IsControlledDrug, request.Status,
            request.Schedules.Select(s => (s.Dose, s.FType, s.Times, s.IntervalDays, s.DaysOfWeek, s.AnchorDate, s.EffectiveFrom, s.EffectiveTo, s.Sequence)), 
            request.CreatedAt);
            if (medicationOrder.IsFailure)
            {
                return Result.Failure<MedicationOrder>(medicationOrder.Error);
            }
            await _medicationOrder.AddAsync(medicationOrder.Value, token);
            await unitOfWork.SaveChangesAsync(token);
            return Result.Success(medicationOrder.Value);
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

       

        public async Task<Result<MedicationAdministration>> RecordAministration(Guid orderId,DateTime scheduledFor, Guid outcomeCodeId,        
        Guid staffId, CancellationToken token, Guid? witnessId = null, string? notes = null)
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
            var administration = MedicationAdministration.Create(orderId, scheduledFor,DateTime.Now, outcomeCodeId, staffId,witnessId,notes);
            if (administration.IsFailure)
            {
                return Result.Failure<MedicationAdministration>(administration.Error);
            }
            await _medicalAdministrationRepository.AddAsync(administration.Value);
            await unitOfWork.SaveChangesAsync(token);
            return administration;  
                
        }

       
    }
}