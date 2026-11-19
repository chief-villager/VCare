using VCare.SharedKernel.Results;

namespace Medications.Application.Services.Interfaces
{
    // Every order-level call takes the patient from the route as well as the order:
    // an order reached through another resident's URL is treated as not found, so
    // a dose can never be signed against the wrong person.
    public interface IMedicationService
    {
        Task<Result<CreateMedicationOrderResponse>> CreateMedicationOrderAsync(Guid patientId, CreateMedicationOrderRequest request, CancellationToken token);
        Task<Result>UpdateMedicationOrderStatusAsync(Guid patientId, Guid medicationOrderId, string status, CancellationToken token);
        Task<Result<MedicationAdministrationResponse>> RecordAministration(Guid patientId, Guid orderId, Guid scheduleId,
            RecordAdministrationRequest request, Guid staffId, CancellationToken token);
        Task<Result<MedicationAdministrationResponse>> RecordPrnAdministration(Guid patientId, Guid orderId,
            RecordPrnAdministrationRequest request, Guid staffId, CancellationToken token);
        Task<Result<MedicationOrderResponse>> GetMedicationOrder(Guid patientId, Guid orderId, CancellationToken token);
        Task<Result<IEnumerable<DueSlot>>> GetDueForDay(Guid patientId, DateOnly day, CancellationToken token);
        Task<Result<IEnumerable<DueSlot>>> GetOutstandingForDay(Guid patientId, DateOnly day, CancellationToken token);
    }
}
