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
    internal class Staff : AggregateRoot<StaffId>
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
            var id = StaffId.New();
            var newStaff =  new Staff
            {
                Id = StaffId.New(),
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phoneNumber,
                Role = role,
                Address = address,
                UserName = username,
                CareHomeId = new CareHomeId(careHomeId),
                CreatedAt = DateTime.Now,
            };
            newStaff.Raise(new StaffCreatedEvent(email,username));
            return newStaff;
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
                Role = role;
            if (!string.IsNullOrWhiteSpace(address))
                Address = address;
            ModifiedAt = DateTime.Now;
        }

        public void RaisePassWordUpdatedEvent(string email, string username, string url)
        {
            Raise(new ResetPasswordEvent(email, username, url));
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