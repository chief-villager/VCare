using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VCare.SharedKernel.Domain
{
    public class StorageEntity
    {
        public string BaseUrl { get; private set; } = null!;
        public string BucketName { get; private set; } = null!;
    }
}