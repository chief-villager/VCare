namespace VCare.SharedKernel.Abstractions
{
    /// <summary>
    /// Lets a module check a patient it only knows by id. Patients owns the
    /// patient, so a care plan, an order or a visit cannot look the row up itself;
    /// without this, any id the client sends (a typo, or another home's resident)
    /// was accepted and records were filed against someone who isn't there.
    /// </summary>
    public interface IPatientDirectory
    {
        /// <summary>True only when the patient exists in the caller's care home.</summary>
        Task<bool> ExistsAsync(Guid patientId, CancellationToken cancellationToken);
    }

    /// <summary>The same check for staff, owned by the Staffs module.</summary>
    public interface IStaffDirectory
    {
        /// <summary>True only when the staff member exists in the caller's care home.</summary>
        Task<bool> ExistsAsync(Guid staffId, CancellationToken cancellationToken);
    }
}
