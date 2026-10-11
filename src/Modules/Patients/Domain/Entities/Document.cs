using System;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace Patients.Domain.Entities
{
    internal class PatientDocument
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public PatientId PatientId { get; private set; }
        public string FileName { get; private set; } = null!;
        public string StorageKeyName { get; private set; } = null!;
        private PatientDocument()
        {
           
        }
        public static Result<PatientDocument> Create(string fileName, string storageKeyName, PatientId patientId)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return Result.Failure<PatientDocument>(new Error("400", "File name is required."));
            if (string.IsNullOrWhiteSpace(storageKeyName))
                return Result.Failure<PatientDocument>(new Error("400", "Storage key name is required."));
            return Result.Success(new PatientDocument
            {
                FileName = fileName,
                StorageKeyName = storageKeyName,
                PatientId = patientId
            });
        }
    }
}