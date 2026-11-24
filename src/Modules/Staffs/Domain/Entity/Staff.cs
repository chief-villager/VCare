using VCare.SharedKernel.Auditing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;

namespace Staffs.Domain.Entity
{
    internal class Staff : AggregateRoot<StaffId>, IAudited
    {
        public string FirstName { get; private set; } = null!;
        public string LastName { get; private set; } = null!;
        public string Email { get; private set; } = null!;
        public string PhoneNumber { get; private set; } = null!;
        public string Role { get; private set; } = null!;
        public string Address { get; private set; } = null!;
        public string UserName {get; private set;} = null!;
        public string FullName => $"{FirstName} {LastName}";
        public CareHomeId CareHomeId {get; private set;}
        public string? ProfilePictureUrl {get; private set;}
        public DateTime ModifiedAt {get; private set;} = DateTime.Now;
        public DateTime CreatedAt {get; private set;} = DateTime.Now;
        public List<StaffDocument> StaffDocuments {get; private set;} = [];

        // Set when the staff member leaves or is suspended. Kept rather than deleted:
        // their name stays on every dose, visit and record they signed.
        public DateTime? DeactivatedAt {get; private set;}
        public Guid? DeactivatedByStaffId {get; private set;}
        public string? DeactivationReason {get; private set;}
        public bool IsActive => DeactivatedAt is null;
        public const int DeactivationReasonMaxLength = 500;

        /// <summary>The role's canonical name ("Admin", "Carer"), or null if it is not one.</summary>
        public static string? CanonicalRole(string? role) =>
            Enum.TryParse<Staffs.Domain.RoleEnum>(role?.Trim(), ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
                ? parsed.ToString()
                : null;

        public Result Deactivate(Guid byStaffId, string? reason, DateTime at)
        {
            if (!IsActive)
                return Result.Failure(Error.Conflict("Staffs.AlreadyDeactivated", "This staff member is already deactivated."));
            if (string.IsNullOrWhiteSpace(reason))
                return Result.Failure(Error.Validation("Staffs.DeactivationReasonRequired", "Say why the staff member is being deactivated."));
            if (reason.Trim().Length > DeactivationReasonMaxLength)
                return Result.Failure($"The reason can be at most {DeactivationReasonMaxLength} characters.");
            DeactivatedAt = at;
            DeactivatedByStaffId = byStaffId;
            DeactivationReason = reason.Trim();
            ModifiedAt = at;
            return Result.Success();
        }

        public Result Reactivate(DateTime at)
        {
            if (IsActive)
                return Result.Failure(Error.Conflict("Staffs.NotDeactivated", "This staff member is not deactivated."));
            DeactivatedAt = null;
            DeactivatedByStaffId = null;
            DeactivationReason = null;
            ModifiedAt = at;
            return Result.Success();
        }
        public IReadOnlyList<StaffDocument> GetDocuments() => StaffDocuments.AsReadOnly();
        public Result UpdateProfilePicture(string profilePictureUrl)
        {
            if (string.IsNullOrWhiteSpace(profilePictureUrl))
            {
               return Result.Failure(new Error("400", "Profile picture URL cannot be empty", ErrorKind.Validation));
            }
            ProfilePictureUrl = profilePictureUrl;
            ModifiedAt = DateTime.Now;
            return Result.Success();
        }


        private Staff() { } // Required by EF Core

        public static Staff Create(string firstName, string lastName, string email, string phoneNumber, string role, string address, string username, Guid careHomeId)
        {
            return new Staff
            {
                Id = StaffId.New(),
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phoneNumber,
                Role = CanonicalRole(role) ?? role,
                Address = address,
                UserName = username,
                CareHomeId = new CareHomeId(careHomeId),
                CreatedAt = DateTime.Now,
            };
        }

        public void Update(string? firstName = null, string? lastName = null, string? email = null, string? phoneNumber = null, string? role = null, string? address = null)
        {
           if (!string.IsNullOrWhiteSpace(firstName))
                FirstName = firstName;
            if (!string.IsNullOrWhiteSpace(lastName))
                LastName = lastName;
            if (!string.IsNullOrWhiteSpace(email))
                Email = email;
            if (!string.IsNullOrWhiteSpace(phoneNumber))
                PhoneNumber = phoneNumber;
            if (!string.IsNullOrWhiteSpace(role))
                Role = CanonicalRole(role) ?? role;
            if (!string.IsNullOrWhiteSpace(address))
                Address = address;
            ModifiedAt = DateTime.Now;
        }

        public  Result<StaffDocument> AddDocument(string fileName, string storageKeyName)
        {

            var documentResult = StaffDocument.Create(fileName, storageKeyName, Id);
            if (documentResult.IsFailure)
            {
                return Result.Failure<StaffDocument>(documentResult.Error);
            }
            StaffDocuments.Add(documentResult.Value);
            ModifiedAt = DateTime.Now;
            return Result.Success(documentResult.Value);
        }
    
  
        
    }

   
}