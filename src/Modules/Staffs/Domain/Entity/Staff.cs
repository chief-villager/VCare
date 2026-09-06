using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;

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
        public string FullName => $"{FirstName} {LastName}";

        private Staff() { } // Required by EF Core

        public static Staff Create(string firstName, string lastName, string email, string phoneNumber, string role, string address)
        {
            var id = StaffId.New();
            return new Staff
            {
                Id = StaffId.New(),
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phoneNumber,
                Role = role,
                Address = address
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
                Role = role;
            if (!string.IsNullOrWhiteSpace(address))
                Address = address;
        }
    
        
    }
}