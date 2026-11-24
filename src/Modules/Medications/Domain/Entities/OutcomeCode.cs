using VCare.SharedKernel.Results;

namespace Medications.Domain.Entities
{
    internal class OutcomeCode
    {
        // Seeded reference rows (see OutcomeCodeConfiguration). Code that records or
        // recognises one of these outcomes uses the id rather than looking it up by name.
        public static readonly Guid MissedId = Guid.Parse("db5662a7-5f67-4890-841f-c567d890e123");
        public static readonly Guid TakenId = Guid.Parse("e2b8c0d4-5f67-4890-b234-c567d890e123");
        public static readonly Guid RefusedId = Guid.Parse("f3c9d1e5-6078-4901-c345-d678e901f234");

        // Why a dose was not given, beyond refused. The letters follow the common
        // MAR convention of one letter per reason; a home whose paper chart uses
        // different letters should agree a mapping before it switches over.
        public static readonly Guid WithheldId = Guid.Parse("0b6f5e7a-91c2-4d3e-8f10-2a3b4c5d6e01");
        public static readonly Guid AsleepId = Guid.Parse("0b6f5e7a-91c2-4d3e-8f10-2a3b4c5d6e02");
        public static readonly Guid InHospitalId = Guid.Parse("0b6f5e7a-91c2-4d3e-8f10-2a3b4c5d6e03");
        public static readonly Guid OnLeaveId = Guid.Parse("0b6f5e7a-91c2-4d3e-8f10-2a3b4c5d6e04");
        public static readonly Guid OutOfStockId = Guid.Parse("0b6f5e7a-91c2-4d3e-8f10-2a3b4c5d6e05");
        public static readonly Guid VomitedId = Guid.Parse("0b6f5e7a-91c2-4d3e-8f10-2a3b4c5d6e06");

        public  Guid Id {get; private set;}
        public string Name {get; private set;} = null!; //Refused
        public string DisplayLetter { get; private set; }  = null!;// "R"


        private OutcomeCode(){}

        private OutcomeCode(string name, string displayLetter)
        {
            Id = Guid.NewGuid();
            Name = name;
            DisplayLetter = displayLetter;

        }

        public static Result<OutcomeCode> Create(string name, string displayLetter)
        {
            if (string.IsNullOrEmpty(name))
            {
               return Result.Failure<OutcomeCode>("name is required");
            }
            if (string.IsNullOrWhiteSpace(displayLetter))
            {
                return Result.Failure<OutcomeCode>("display name is required");
            }
            var outcome = new OutcomeCode(name, displayLetter);
            return  Result.Success(outcome);
        }

        /// <summary>A reference row with a fixed id: the seeded outcomes.</summary>
        public static OutcomeCode Reference(Guid id, string name, string displayLetter) =>
            new() { Id = id, Name = name, DisplayLetter = displayLetter };

        /// <summary>The outcomes every care home starts with.</summary>
        public static IReadOnlyList<OutcomeCode> Seeded { get; } =
        [
            Reference(RefusedId, "Refused", "R"),
            Reference(TakenId, "Taken", "T"),
            Reference(MissedId, "Missed", "M"),
            Reference(WithheldId, "Withheld", "W"),
            Reference(AsleepId, "Asleep", "S"),
            Reference(InHospitalId, "In hospital", "H"),
            Reference(OnLeaveId, "On leave", "L"),
            Reference(OutOfStockId, "Out of stock", "O"),
            Reference(VomitedId, "Vomited", "V"),
        ];

        /// <summary>
        /// The resident had the medicine. Vomited counts: the dose went in, and
        /// re-dosing is a clinical decision, not something a limit should wave through.
        /// What PRN intervals and 24-hour limits are counted against.
        /// </summary>
        public static bool CountsAsGiven(Guid outcomeId) => outcomeId == TakenId || outcomeId == VomitedId;

        /// <summary>
        /// A controlled drug was taken out of the cupboard: given, refused (and so
        /// destroyed) or vomited. Each needs a second signature.
        /// </summary>
        public static bool NeedsControlledDrugWitness(Guid outcomeId) =>
            outcomeId == TakenId || outcomeId == RefusedId || outcomeId == VomitedId;

        /// <summary>A dose deliberately not given needs the clinical reason on the record.</summary>
        public static bool NeedsReason(Guid outcomeId) => outcomeId == WithheldId;
    }
}
