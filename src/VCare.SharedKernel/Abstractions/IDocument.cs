using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Results;

namespace VCare.SharedKernel.Abstractions
{
    public interface IDocumentService
    {
        // Uploads take the request's file content as a stream. A path would be read
        // from the API server's own disk, which never holds the uploaded file.
        Task<Result<string>> WritingAnObjectAsync(string bucketName, Stream content, string fileName, string contentType, string careHomeId, CancellationToken cancellationToken = default);
        Task<(Stream Content, string ContentType)> GetObjectAsync(string objectKey, string sourceBucket, CancellationToken cancellationToken = default);
        Task<Result<string>> UploadProfileImageAsync(string bucketName, Stream content, string fileName, string contentType, string careHomeId, CancellationToken cancellationToken = default);
        Task<(Stream Content, string ContentType)> GetProfileImageAsync(string objectKey, string sourceBucket, CancellationToken cancellationToken = default);
    }
}
