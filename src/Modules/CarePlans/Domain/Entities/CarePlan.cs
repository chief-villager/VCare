using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CarePlans.Domain.Entities;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;

namespace VCare.Modules.CarePlans.Domain.Entities
{
    public sealed class CarePlan : AggregateRoot
    {
        public Guid PatientId { get; private set; }
        public List<Diagnosis> Diagnoses { get; private set; } = [];
        public List<PatientGoals> Goals { get; private set; } = [];
        public List<Intervention> Intervention {get; private set;} = [];
        public DateOnly CreatedDate {get; private set;}
        public DateOnly ModifiedDate {get; set;}
        public Guid StaffId {get; private set;}

        private CarePlan() { }

        public static Result<CarePlan> Create(
            Guid patientId,
            Guid staffId,
            IEnumerable<string>? diagnoses = null,
            IEnumerable<string>? goals = null,
            IEnumerable<(string Description, bool Implemented)>? interventions = null)
        {
            if (patientId == Guid.Empty)
                return Result.Failure<CarePlan>("Patient is required.");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var carePlan = new CarePlan
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                StaffId = staffId,
                CreatedDate = today,
                ModifiedDate = today,
            };

            carePlan.ReplaceDetails(diagnoses, goals, interventions);
            return Result.Success(carePlan);
        }

        public Result Update(
            IEnumerable<string>? diagnoses = null,
            IEnumerable<string>? goals = null,
            IEnumerable<(string Description, bool Implemented)>? interventions = null)
        {
            ReplaceDetails(diagnoses, goals, interventions);
            ModifiedDate = DateOnly.FromDateTime(DateTime.UtcNow);
            return Result.Success();
        }

        private void ReplaceDetails(
            IEnumerable<string>? diagnoses,
            IEnumerable<string>? goals,
            IEnumerable<(string Description, bool Implemented)>? interventions)
        {
            if (diagnoses is not null)
                Diagnoses = [.. diagnoses.Select(d => Diagnosis.Create(Id, d))];

            if (goals is not null)
                Goals = [.. goals.Select(g => new PatientGoals(Id, g))];

            if (interventions is not null)
                Intervention = [.. interventions.Select(i => new Intervention(Id, i.Description, i.Implemented))];
        }
    }

}
