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
        ];
    }
}
