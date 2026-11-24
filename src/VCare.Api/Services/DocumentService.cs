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

        // The client's content type is ignored: UploadPolicy decides from the extension.
        public Task<Result<string>> WritingAnObjectAsync(string bucketName, Stream content, string fileName, string contentType, string careHomeId, CancellationToken cancellationToken = default) =>
            UploadAsync(bucketName, content, fileName, UploadPolicy.Documents, careHomeId, "documents", cancellationToken);

        public Task<Result<string>> UploadProfileImageAsync(string bucketName, Stream content, string fileName, string contentType, string careHomeId, CancellationToken cancellationToken = default) =>
            UploadAsync(bucketName, content, fileName, UploadPolicy.ProfileImages, careHomeId, "profiles", cancellationToken);

        // Streamed straight from S3 to the caller. Copying into a MemoryStream first
        // held every download whole in the server's memory, so a few large scans at
        // once could exhaust it. The response is disposed with the stream.
        private async Task<(Stream Content, string ContentType)> DownloadAsync(string objectKey, string sourceBucket, string fallbackContentType, CancellationToken cancellationToken)
        {
            var request = new GetObjectRequest
            {
                BucketName = sourceBucket,
                Key = objectKey
            };

            try
            {
                var response = await _s3Client.GetObjectAsync(request, cancellationToken);
                return (new OwningStream(response.ResponseStream, response), response.Headers.ContentType ?? fallbackContentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving {Key} from S3 bucket {Bucket}", objectKey, sourceBucket);
                throw;
            }
        }

        /// <summary>Reads from the S3 body and disposes the whole response when the caller is done.</summary>
        private sealed class OwningStream(Stream inner, IDisposable owner) : Stream
        {
            public override bool CanRead => inner.CanRead;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => inner.Length;
            public override long Position { get => inner.Position; set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
                inner.ReadAsync(buffer, offset, count, cancellationToken);
            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
                inner.ReadAsync(buffer, cancellationToken);
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    inner.Dispose();
                    owner.Dispose();
                }
                base.Dispose(disposing);
            }
        }

        private async Task<Result<string>> UploadAsync(string bucketName, Stream content, string fileName,
            IReadOnlyDictionary<string, string> allowed, string careHomeId, string folder, CancellationToken cancellationToken)
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
            var contentType = UploadPolicy.ContentTypeFor(safeName, allowed);
            if (contentType.IsFailure)
            {
                return Result.Failure<string>(contentType.Error);
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
                    ContentType = contentType.Value,
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
