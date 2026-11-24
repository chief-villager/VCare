using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Staffs.Domain.Entity;

namespace Staffs.Application.Abstraction
{
    internal interface IStaffRepository
    {
        Task<Staff?> GetByIdAsync(VCare.SharedKernel.Abstractions.StaffId id, CancellationToken cancellationToken = default);
        Task<Staff?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        Task AddAsync(Staff staff, CancellationToken cancellationToken = default);
        
        void Update(Staff staff);
        Task<int> SaveChangesAsync( CancellationToken cancellationToken = default);

        /// <summary>Ends the request's work: dispatches domain events, saves and
        /// commits, all in one transaction. Identity's own saves go
        /// through <see cref="SaveChangesAsync"/> and dispatch nothing.</summary>
        Task<int> CommitandSaveAsync(CancellationToken cancellationToken = default);

        /// <summary>Active admins in the caller's care home: a home must always keep one.</summary>
        Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken = default);

        /// <summary>True when the storage key is one of this staff member's documents
        /// or their profile image: the only objects a download for them may return.</summary>
        Task<bool> OwnsStorageKeyAsync(VCare.SharedKernel.Abstractions.StaffId staffId, string storageKey, CancellationToken cancellationToken = default);

    }
}