using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Results;

namespace VCare.SharedKernel.Abstractions
{
    public interface IDocumentService
    {
        Task<Result<string>> WritingAnObjectAsync(string bucketName, string filepath, string careHomeId, CancellationToken cancellationToken = default);
        Task<(Stream Content, string ContentType)> GetObjectAsync(string objectKey, string sourceBucket, CancellationToken cancellationToken = default);
        Task<Result<string>> UploadProfileImageAsync(string bucketName, string filepath, string careHomeId, CancellationToken cancellationToken = default);
        Task<(Stream Content, string ContentType)> GetProfileImageAsync(string objectKey, string sourceBucket, CancellationToken cancellationToken = default);


    }
}