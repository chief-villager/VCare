using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;

namespace VCare.Api.Services
{
    // The S3 client is a singleton registered in Program.cs: it is thread-safe and
    // holds the HTTP connection pool and resolved credentials, so one is shared by
    // every request rather than rebuilt (and leaked) per scope.
    public class DocumentService(ILogger<DocumentService> logger, IAmazonS3 s3Client) : IDocumentService
    {
        private readonly ILogger<DocumentService> _logger = logger;
        private readonly IAmazonS3 _s3Client = s3Client;

        public Task<(Stream Content, string ContentType)> GetObjectAsync(string objectKey, string sourceBucket, CancellationToken cancellationToken = default) =>
            DownloadAsync(objectKey, sourceBucket, "application/octet-stream", cancellationToken);

        public Task<(Stream Content, string ContentType)> GetProfileImageAsync(string objectKey, string sourceBucket, CancellationToken cancellationToken = default) =>
            DownloadAsync(objectKey, sourceBucket, "image/jpeg", cancellationToken);

        public Task<Result<string>> WritingAnObjectAsync(string bucketName, Stream content, string fileName, string contentType, string careHomeId, CancellationToken cancellationToken = default) =>
            UploadAsync(bucketName, content, fileName, contentType, careHomeId, "documents", cancellationToken);

        public Task<Result<string>> UploadProfileImageAsync(string bucketName, Stream content, string fileName, string contentType, string careHomeId, CancellationToken cancellationToken = default) =>
            UploadAsync(bucketName, content, fileName, contentType, careHomeId, "profiles", cancellationToken);

        private async Task<(Stream Content, string ContentType)> DownloadAsync(string objectKey, string sourceBucket, string fallbackContentType, CancellationToken cancellationToken)
        {
            var request = new GetObjectRequest
            {
                BucketName = sourceBucket,
                Key = objectKey
            };
            var memoryStream = new MemoryStream();

            try
            {
                using var response = await _s3Client.GetObjectAsync(request, cancellationToken);
                await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);

                // CopyToAsync leaves the position at the end; without this the
                // caller streams back an empty body.
                memoryStream.Position = 0;
                return (memoryStream, response.Headers.ContentType ?? fallbackContentType);
            }
            catch (Exception ex)
            {
                await memoryStream.DisposeAsync();
                _logger.LogError(ex, "Error retrieving {Key} from S3 bucket {Bucket}", objectKey, sourceBucket);
                throw;
            }
        }

        private async Task<Result<string>> UploadAsync(string bucketName, Stream content, string fileName, string contentType,
            string careHomeId, string folder, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(bucketName))
            {
                return Result.Failure<string>(new Error("BucketNameRequired", "Bucket name is required", ErrorKind.Validation));
            }
            if (content is null)
            {
                return Result.Failure<string>(new Error("FileContentRequired", "File content is required", ErrorKind.Validation));
            }
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return Result.Failure<string>(new Error("FileNameRequired", "File name is required", ErrorKind.Validation));
            }
            if (string.IsNullOrWhiteSpace(careHomeId))
            {
                return Result.Failure<string>(new Error("CareHomeIdRequired", "Care home ID is required", ErrorKind.Validation));
            }

            // The name comes from the client: keep only its last segment so it
            // cannot steer the key into another care home's prefix.
            var safeName = Path.GetFileName(fileName).Trim();
            if (safeName.Length == 0)
            {
                return Result.Failure<string>(new Error("FileNameRequired", "File name is required", ErrorKind.Validation));
            }
            var keyName = $"{careHomeId}/{folder}/{Guid.NewGuid():N}_{safeName}";

           
            try
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = keyName,
                    InputStream = content,
                    // The caller owns the stream and disposes it.
                    AutoCloseStream = false,
                    ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                };
                await _s3Client.PutObjectAsync(putRequest, cancellationToken);
                return Result.Success(keyName);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "Error uploading {Key} to S3 bucket {Bucket}", keyName, bucketName);
                throw;
            }
        }
    }
}
