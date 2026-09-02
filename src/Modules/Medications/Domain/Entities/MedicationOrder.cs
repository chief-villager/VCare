using Medications.Domain.Enum;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;

namespace Medications.Domain.Entities
{
    internal sealed class MedicationOrder : AggregateRoot
    {
        public Guid PatientId { get; private set; }
        public string Medication { get; private set; } = null!; 
        public MedicationRouteEnum Route { get; private set; }        // oral, topical, s/c...
        public string? Instructions { get; private set; }  // "take with food"
        public DateOnly StartDate { get; private set; }
        public DateOnly? EndDate { get; private set; }  // null = ongoing
        public string Prescriber { get; private set; } = null!;
        public bool IsPrn { get; private set; }          // "as required"
        public string? PrnIndication { get; private set; }   // "for pain"
        public int? PrnMinIntervalMinutes { get; private set; }
        public string? PrnMaxDose24h { get; private set; }
        public bool IsControlledDrug { get; private set; }
        public OrderStatus Status { get; private set; } 
        public ICollection<MedicationAdministration> Administrations { get; private set; } = [];
        public ICollection<DoseSchedule> Schedule { get; private set; }= [];


        private MedicationOrder(){}
        

        private MedicationOrder(
            Guid patientId, string medication, 
            MedicationRouteEnum route, string? instruction, DateOnly startDate,
            DateOnly? endDate, string prescriber, bool isPrn, string? prnIndication,
            bool isControlledDrug, OrderStatus orderStatus,  
             List<DoseSchedule> doseSchedule  )
        {
            Id = Guid.NewGuid();
            PatientId = patientId;
            Medication = medication;
            Route = route;
            Instructions = instruction;
            StartDate = startDate;
            EndDate = endDate;
            Prescriber = prescriber;
            IsPrn = isPrn ;
            PrnIndication = prnIndication;
            IsControlledDrug = isControlledDrug;
            Status = orderStatus;
            Schedule = doseSchedule;
        }

        public static Result<MedicationOrder> Record(
            Guid patientId, string medication, 
            MedicationRouteEnum route, string? instruction, DateOnly startDate,
            DateOnly? endDate, string prescriber, bool isPrn, string? prnIndication,
            bool isControlledDrug, OrderStatus orderStatus,  
             List<DoseSchedule> doseSchedule)

        {
             if (patientId == Guid.Empty)
                return Result.Failure<MedicationOrder>("Patient is required.");

            if (string.IsNullOrWhiteSpace(medication))
                return Result.Failure<MedicationOrder>("Medication is required.");

            if (endDate.HasValue && endDate.Value < startDate)
                return Result.Failure<MedicationOrder>(
                    "End date cannot be before start date.");

            if (isPrn && string.IsNullOrWhiteSpace(prnIndication))
                return Result.Failure<MedicationOrder>(
                    "PRN indication is required for PRN medication.");
            var order = new MedicationOrder( patientId, medication, route, instruction, startDate,
            endDate, prescriber, isPrn, prnIndication, isControlledDrug, orderStatus, doseSchedule);
            return Result.Success(order) ;
            
            
        }

        public Result UpdateStatus(string status)
        {
            if( status == nameof(OrderStatus.Suspended) )
            {
                Status = OrderStatus.Suspended;
                return Result.Success();
            }
            if ( status == nameof(OrderStatus.Discontinued))
            {
                Status = OrderStatus.Discontinued;
                return Result.Success();
            }
            return Result.Failure("Invalid OrderStatus");
           
        }

    }
}