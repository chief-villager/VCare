using VCare.SharedKernel.Auditing;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;

namespace Medications.Domain.Entities
{
    internal class MedicationAdministration: Entity<MedicationAdministrationId>, IAudited
    {

        public PatientId PatientId {get; private set;}
        public CareHomeId CareHomeId { get; private set; }
        public MedicationOrderId MedicationOrderId { get; private set; }

        public DateTime? ScheduledFor { get; private set; }   // null for PRN
        public DateTime? AdministeredAt { get; private set; }
        public Guid OutcomeCodeId { get; private set; }       // Given, Refused, Omitted...
        public Guid AdministeredByStaffId { get; private set; }
        public string? Notes { get; private set; }

        // Recorded after the dose's on-time window closed. A late entry is still
        // the truth about what happened; the flag is what lets a reviewer find it.
        public bool IsLate { get; private set; }

        // Why the entry was made: why a PRN dose was given ("pain in left hip"),
        // or why a scheduled dose was recorded late.
        public string? Reason { get; private set; }

        // The dose actually given. Required for PRN, where the order allows a range
        // ("1-2 tablets") and the amount given decides what is still allowed today.
        public string? DoseGiven { get; private set; }

        // The second member of staff who saw a controlled drug prepared and given
        // (or refused and destroyed). Required on a controlled-drug order.
        public Guid? WitnessedByStaffId { get; private set; }

        public DateTime CreatedAt { get; private set; }
        public DateTime? ModifiedAt { get; private set; }

        // Column sizes, checked here so an over-long entry is a 400 the carer can
        // fix rather than a database error at save.
        public const int ReasonMaxLength = 500;
        public const int DoseGivenMaxLength = 100;

        private MedicationAdministration(){}


        public static Result<MedicationAdministration> Create(Guid medicationOrderId, Guid patientId, Guid careHomeId, DateTime? scheduledFor,
        DateTime administeredAt, Guid outcomeId, Guid administeredByStaffId,
         string? notes, DateTime recordedAt, bool isLate = false, string? reason = null, string? doseGiven = null,
         Guid? witnessedByStaffId = null)
        {
            if (medicationOrderId == Guid.Empty)
            {
                return Result.Failure<MedicationAdministration>("medicationOrderId id required");
            }
            if ( outcomeId == Guid.Empty)
            {
                return Result.Failure<MedicationAdministration>("outcomeId id required");
            }
            if ( administeredByStaffId == Guid.Empty)
            {
                return Result.Failure<MedicationAdministration>("StaffId id required");
            }
            if (patientId == Guid.Empty )
            {
                return Result.Failure<MedicationAdministration>("patientId id required");
            };
            if (careHomeId == Guid.Empty)
            {
                return Result.Failure<MedicationAdministration>("careHomeId is required");
            }
            if (reason?.Length > ReasonMaxLength)
            {
                return Result.Failure<MedicationAdministration>($"The reason can be at most {ReasonMaxLength} characters.");
            }
            if (doseGiven?.Length > DoseGivenMaxLength)
            {
                return Result.Failure<MedicationAdministration>($"The dose given can be at most {DoseGivenMaxLength} characters.");
            }

            var administration = new MedicationAdministration
            {
                Id = MedicationAdministrationId.New(),
                MedicationOrderId = new MedicationOrderId(medicationOrderId),
                PatientId = new PatientId(patientId),
                CareHomeId = new CareHomeId(careHomeId),
                ScheduledFor = scheduledFor,
                AdministeredAt = administeredAt,
                OutcomeCodeId = outcomeId,
                AdministeredByStaffId = administeredByStaffId,
                Notes = notes,
                IsLate = isLate,
                Reason = reason,
                DoseGiven = doseGiven,
                WitnessedByStaffId = witnessedByStaffId,
                CreatedAt = recordedAt
            };
            return Result.Success(administration);

        }

        /// <summary>Stands in for a staff member on records nobody signed: the
        /// missed-dose recorder writes them, not a person.</summary>
        public static readonly Guid SystemStaffId = Guid.Empty;

        /// <summary>
        /// A scheduled dose whose window closed unsigned. Nothing was given, so
        /// there is no administration time. <paramref name="recordedByStaffId"/>
        /// is <see cref="SystemStaffId"/> when the background recorder found it.
        /// </summary>
        public static MedicationAdministration RecordMissed(MedicationOrder order, DateTime scheduledFor,
            Guid recordedByStaffId, string notes, DateTime recordedAt) =>
            new()
            {
                Id = MedicationAdministrationId.New(),
                MedicationOrderId = order.Id,
                PatientId = order.PatientId,
                CareHomeId = order.CareHomeId,
                ScheduledFor = scheduledFor,
                AdministeredAt = null,
                OutcomeCodeId = OutcomeCode.MissedId,
                AdministeredByStaffId = recordedByStaffId,
                Notes = notes,
                CreatedAt = recordedAt
            };

        /// <summary>
        /// A scheduled dose due while the resident was away (in hospital, on leave):
        /// the home could not give it, so it is recorded with that outcome, not as missed.
        /// </summary>
        public static MedicationAdministration RecordAway(MedicationOrder order, DateTime scheduledFor,
            Guid outcomeId, string notes, DateTime recordedAt) =>
            new()
            {
                Id = MedicationAdministrationId.New(),
                MedicationOrderId = order.Id,
                PatientId = order.PatientId,
                CareHomeId = order.CareHomeId,
                ScheduledFor = scheduledFor,
                AdministeredAt = null,
                OutcomeCodeId = outcomeId,
                AdministeredByStaffId = SystemStaffId,
                Notes = notes,
                CreatedAt = recordedAt
            };

        public bool IsMissed => OutcomeCodeId == OutcomeCode.MissedId;

        /// <summary>
        /// Replaces a Missed placeholder with what actually happened. The placeholder
        /// was written by the system because nobody signed in time, not by a person
        /// as a clinical entry, so the carer's account is the record. CreatedAt keeps
        /// when the placeholder was written, and IsLate plus ModifiedAt keep that the
        /// account came afterwards.
        /// </summary>
        public Result ResolveMissed(Guid outcomeId, Guid staffId, DateTime administeredAt, string reason,
            string? notes, DateTime recordedAt, Guid? witnessedByStaffId = null)
        {
            if (!IsMissed)
                return Result.Failure(Error.Conflict("Medications.DoseAlreadyRecorded", "This dose is already recorded."));
            if (outcomeId == Guid.Empty || outcomeId == OutcomeCode.MissedId)
                return Result.Failure("A real outcome is required.");
            if (staffId == Guid.Empty)
                return Result.Failure("StaffId id required");
            if (string.IsNullOrWhiteSpace(reason))
                return Result.Failure("A reason is required.");
            if (reason.Trim().Length > ReasonMaxLength)
                return Result.Failure($"The reason can be at most {ReasonMaxLength} characters.");

            OutcomeCodeId = outcomeId;
            AdministeredByStaffId = staffId;
            AdministeredAt = administeredAt;
            Reason = reason.Trim();
            Notes = notes;
            WitnessedByStaffId = witnessedByStaffId;
            IsLate = true;
            ModifiedAt = recordedAt;
            return Result.Success();
        }



    }
}
