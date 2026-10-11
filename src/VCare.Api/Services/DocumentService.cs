using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace VCare.Api.Services
{
    public class DocumentService(ILogger<DocumentService> logger) : IDocumentService
    {
        public async Task<(Stream Content, string ContentType)> GetObjectAsync(string objectKey, string sourceBucket, CancellationToken cancellationToken = default)
        {
            
            using var client = new AmazonS3Client(Amazon.RegionEndpoint.USEast1);
            
            var request = new GetObjectRequest
            {
                BucketName = sourceBucket,
                Key = objectKey
            };
            var memoryStream = new MemoryStream();
    
            try
            {
                using var response = await  client.GetObjectAsync(request, cancellationToken);
                await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
                return (memoryStream, response.Headers.ContentType ?? "application/octet-stream");
             
                
            }
            catch (AmazonS3Exception ex)
            {
                await memoryStream.DisposeAsync(); // Dispose the memory stream in case of an exception
                logger.LogError($"Error retrieving object from S3: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError($"General error retrieving object from S3: {ex.Message}");
                throw;
            }
        }

        public async Task<(Stream Content, string ContentType)> GetProfileImageAsync(string objectKey, string sourceBucket, CancellationToken cancellationToken = default)
        {
             using var client = new AmazonS3Client(Amazon.RegionEndpoint.USEast1);
            
            var request = new GetObjectRequest
            {
                BucketName = sourceBucket,
                Key = objectKey
            };
            var memoryStream = new MemoryStream();
    
            try
            {
                using var response = await  client.GetObjectAsync(request, cancellationToken);
                
                await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0; // Reset the position to the beginning of the stream
                return (memoryStream, response.Headers.ContentType ?? "Image/jpeg");
            }
            catch (AmazonS3Exception ex)
            {
                await memoryStream.DisposeAsync(); // Dispose the memory stream in case of an exception
                logger.LogError($"Error retrieving object from S3: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError($"General error retrieving object from S3: {ex.Message}");
                throw;
            }
        }

        public async Task<Result<string>> UploadProfileImageAsync(string bucketName, string filepath, string careHomeId, CancellationToken cancellationToken = default)
        {
             if (string.IsNullOrWhiteSpace(bucketName))
            {
                return Result.Failure<string>(new Error("BucketNameRequired", "Bucket name is required", ErrorKind.Validation));
            }
            if (string.IsNullOrWhiteSpace(filepath))
            {
                return Result.Failure<string>(new Error("FilePathRequired", "File path is required", ErrorKind.Validation));
            }
            if (string.IsNullOrWhiteSpace(careHomeId))
            {
                return Result.Failure<string>(new Error("CareHomeIdRequired", "Care home ID is required", ErrorKind.Validation));
            }
            using var client = new AmazonS3Client(Amazon.RegionEndpoint.USEast1);
            Random random = new();
            var randomSuffix = random.Next(1000, 9999);
            var filename = System.IO.Path.GetFileName(filepath).Trim();
            careHomeId = careHomeId.Substring(0, Math.Min(30, careHomeId.Length));
            var keyName = $"{careHomeId}/profiles/{randomSuffix}_{filename}";
            try
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    FilePath = filepath,
                    Key = keyName,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                };
                await client.PutObjectAsync(putRequest, cancellationToken);
                return Result.Success(keyName);
            }
            catch (AmazonS3Exception ex)
            {
                logger.LogError($"Error uploading file to S3: {ex.Message}");
                throw;
            }

        }

        public async Task<Result<string>> WritingAnObjectAsync(string bucketName, string filepath, string careHomeId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(bucketName))
            {
                return Result.Failure<string>(new Error("BucketNameRequired", "Bucket name is required", ErrorKind.Validation));
            }
            if (string.IsNullOrWhiteSpace(filepath))
            {
                return Result.Failure<string>(new Error("FilePathRequired", "File path is required", ErrorKind.Validation));
            }
            if (string.IsNullOrWhiteSpace(careHomeId))
            {
                return Result.Failure<string>(new Error("CareHomeIdRequired", "Care home ID is required", ErrorKind.Validation));
            }
            // Create an Amazon S3 client using the default credentials and region
            //the enterd region is not valid for the bucket, so we will use USEast1 as default region for now.
            using var client = new AmazonS3Client(Amazon.RegionEndpoint.USEast1);
            Random random = new();
            var randomSuffix = random.Next(1000, 9999);
            var filename = System.IO.Path.GetFileName(filepath).Trim();
            careHomeId = careHomeId.Substring(0, Math.Min(30, careHomeId.Length));
            var keyName = $"{careHomeId}/profiles/{randomSuffix}_{filename}";

            try
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    FilePath = filepath,
                    Key = keyName,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
                };
                await client.PutObjectAsync(putRequest, cancellationToken);
                return Result.Success(keyName);
            }
            catch (AmazonS3Exception ex)
            {
                logger.LogError($"Error uploading file to S3: {ex.Message}");
                throw;
            }
            
           
            
        }
    }
}