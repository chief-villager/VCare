using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Medications.Domain.Entities;
using VCare.SharedKernel.Results;

namespace Medications.Application.Services.Interfaces
{
    public interface IMedicationService
    {
        Task<Result<CreateMedicationOrderResponse>> CreateMedicationOrderAsync(Guid patientId, CreateMedicationOrderRequest request, CancellationToken token);
        Task<Result>UpdateMedicationOrderStatusAsync(Guid medicationOrderId, string status, CancellationToken token);
        Task<Result<MedicationAdministrationResponse>> RecordAministration(Guid orderId,DateTime scheduledFor, Guid outcomeCodeId,        
        Guid staffId, CancellationToken token, Guid? witnessId = null, string? notes = null);
        Task<Result<MedicationOrderResponse>> GetMedicationOrder(Guid orderId, CancellationToken token);
        Task<Result<IEnumerable<DueSlot>>> GetDueForDay(Guid patientId, DateOnly day, CancellationToken token);
    }
}