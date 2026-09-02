using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading.Tasks;
using Azure.Core;
using VCare.SharedKernel.Results;

namespace Medications.Domain.Entities
{
    internal class OutcomeCode
    {
        internal Guid Id {get; private set;}
        internal string Name {get; private set;} = null!; //Refused
        internal string DisplayLetter { get; private set; }  = null!;// "R"
        internal bool CountsAsMissed { get; private set; }    // for reporting
        internal bool RequiresReason { get; private set; }

        private OutcomeCode(){}

        private OutcomeCode( string name, string displayLetter, bool countsAsMissed
        , bool requiresReason)
        {
            Id = Guid.NewGuid();
            Name = name;
            DisplayLetter = displayLetter;
            CountsAsMissed = countsAsMissed;
            RequiresReason = requiresReason;
        }

        public static Result<OutcomeCode> Create(string name, string displayLetter, bool countsAsMissed
        , bool requiresReason)
        {
            if (string.IsNullOrEmpty(name))
            {
               return Result.Failure<OutcomeCode>("name is required");
            }
            if (string.IsNullOrWhiteSpace(displayLetter))
            {
                return Result.Failure<OutcomeCode>("display name is required");
            }
            var outcome = new OutcomeCode(name, displayLetter, countsAsMissed, requiresReason);
            return  Result.Success(outcome);
        }
    }
}