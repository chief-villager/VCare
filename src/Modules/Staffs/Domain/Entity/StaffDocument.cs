using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace Staffs.Domain.Entity
{

    internal class StaffDocument
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public StaffId StaffId { get; private set; }
        public string FileName { get; private set; } = null!;
        public string StorageKeyName { get; private set; } = null!;
        private StaffDocument()
        {
           
        }
        public static Result<StaffDocument> Create(string fileName, string storageKeyName, StaffId staffId)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return Result.Failure<StaffDocument>(new Error("400", "File name is required."));
            if (string.IsNullOrWhiteSpace(storageKeyName))
                return Result.Failure<StaffDocument>(new Error("400", "Storage key name is required."));
            return Result.Success(new StaffDocument
            {
                FileName = fileName,
                StorageKeyName = storageKeyName,
                StaffId = staffId
            });
        }
    }
}
