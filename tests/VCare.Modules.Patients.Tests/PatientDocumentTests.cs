using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using VCare.Modules.Patients.Application.Abstractions;
using VCare.Modules.Patients.Application.Services;
using VCare.Modules.Patients.Domain.Entities;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;
using Xunit;

namespace VCare.Modules.Patients.Tests;

public class PatientDocumentTests
{
    private static readonly Guid CareHome = Guid.NewGuid();
    private const string OwnLetter = "home-a/documents/1f_hospital-letter.pdf";
    private const string OtherHomesFile = "home-b/documents/9c_consent.pdf";

    private sealed class FakePatients(Patient patient) : IPatientRepository
    {
        public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(id == patient.Id.Value ? patient : null);
        public Task<IReadOnlyList<Patient>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Patient>>([patient]);
        public Task AddAsync(Patient p, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> OwnsStorageKeyAsync(Guid patientId, string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(patientId == patient.Id.Value
                && (patient.ProfilePictureUrl == storageKey || patient.GetDocuments().Any(d => d.StorageKeyName == storageKey)));
    }

    private sealed class NoSaves : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public Guid CareHomeId => CareHome;
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => true;
    }

    /// <summary>Hands back any object it is asked for, as a bucket would; records what was asked.</summary>
    private sealed class Bucket : IDocumentService
    {
        public List<string> Fetched { get; } = [];
        public Task<(Stream Content, string ContentType)> GetObjectAsync(string objectKey, string sourceBucket, CancellationToken cancellationToken = default)
        {
            Fetched.Add(objectKey);
            return Task.FromResult<(Stream, string)>((new MemoryStream([1, 2, 3]), "application/pdf"));
        }
        public Task<(Stream Content, string ContentType)> GetProfileImageAsync(string objectKey, string sourceBucket, CancellationToken cancellationToken = default) =>
            GetObjectAsync(objectKey, sourceBucket, cancellationToken);
        public Task<Result<string>> WritingAnObjectAsync(string bucketName, Stream content, string fileName, string contentType, string careHomeId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<Result<string>> UploadProfileImageAsync(string bucketName, Stream content, string fileName, string contentType, string careHomeId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private static (PatientService Service, Bucket Bucket, Patient Patient) ResidentWithALetter()
    {
        var patient = Patient.Register("Ada", "Lovelace", "Female", "1 Analytical Way", new DateOnly(1940, 5, 12),
            "Daughter", "08000000000", "Anne King", null, CareHome, null).Value;
        patient.AddDocument("Hospital letter", OwnLetter);
        var bucket = new Bucket();
        var service = new PatientService(new FakePatients(patient), new NoSaves(), new FakeCurrentUser(), bucket,
            Options.Create(new StorageEntity { BaseUrl = "https://example.invalid", BucketName = "vcare" }));
        return (service, bucket, patient);
    }

    [Fact]
    public async Task A_key_that_is_not_one_of_the_residents_documents_is_not_fetched()
    {
        var (service, bucket, patient) = ResidentWithALetter();

        var result = await service.GetPatientDocumentsAsync(patient.Id, OtherHomesFile, CancellationToken.None);

        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
        Assert.Empty(bucket.Fetched);
    }

    [Fact]
    public async Task The_residents_own_document_is_fetched()
    {
        var (service, bucket, patient) = ResidentWithALetter();

        var result = await service.GetPatientDocumentsAsync(patient.Id, OwnLetter, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([OwnLetter], bucket.Fetched);
    }
}
