using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace VCare.SharedKernel.Domain
{
    // Setters must be public: the configuration binder silently skips private ones,
    // which left BucketName null and failed every upload. [Required] turns a missing
    // "Storage" section into a startup error (the Staffs module validates on start).
    public class StorageEntity
    {
        [Required] public string BaseUrl { get; set; } = null!;
        [Required] public string BucketName { get; set; } = null!;
        public string Region { get; set; } = "us-east-1";
    }
}
