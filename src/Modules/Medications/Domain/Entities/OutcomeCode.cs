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
        // Seeded reference row (see OutcomeCodeConfiguration). Code that records or
        // recognises a missed dose uses this id rather than looking it up by name.
        public static readonly Guid MissedId = Guid.Parse("db5662a7-5f67-4890-841f-c567d890e123");

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
    }
}