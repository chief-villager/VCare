using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Staffs.Application.Abstraction;
using Staffs.Application.Dto;
using Staffs.Application.Services.Interface;
using Staffs.Domain.Entity;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;

namespace Staffs.Application.Services
{
    internal class StaffService (IStaffRepository staffRepository) : IStaffService
    {
        public async Task<Result<StaffResponse>> GetStaffByIdAsync(StaffId id, CancellationToken cancellationToken)
        {
            var result = await staffRepository.GetByIdAsync(id, cancellationToken);
            if (result is null)
            {
                return Result.Failure<StaffResponse>("Staff not found");
            }
            return Result.Success(new StaffResponse(
                Id: result.Id.Value,
                FirstName: result.FirstName,
                LastName: result.LastName,
                Email: result.Email,
                PhoneNumber: result.PhoneNumber,
                Address: result.Address,
                Role: result.Role
            ));
        }

        public async Task<Result<Guid>> CreateStaffAsync(CreateStaffRequest staffRequest, CancellationToken cancellationToken)
        {
            var staff = Staff.Create(staffRequest.FirstName, staffRequest.LastName, 
            staffRequest.Email, staffRequest.PhoneNumber, staffRequest.Role, staffRequest.Address);
            if (staff is null)
            {
                return Result.Failure<Guid>("Failed to create staff");
            }
            await staffRepository.AddAsync(staff, cancellationToken);
            await staffRepository.SaveChangesAsync(cancellationToken);
            return Result.Success(staff.Id.Value);
        }

        public async Task<Result> UpdateStaffAsync(StaffId staffId,UpdateStaffRequest staff, CancellationToken cancellationToken)
        {
            var existingStaff = await staffRepository.GetByIdAsync( staffId, cancellationToken);
            if (existingStaff is null)
            {
                return Result.Failure("Staff not found");
            }

            existingStaff.Update(staff.FirstName, staff.LastName, staff.Email, staff.PhoneNumber, staff.Role, staff.Address);
            staffRepository.Update(existingStaff);
            await staffRepository.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
   
}