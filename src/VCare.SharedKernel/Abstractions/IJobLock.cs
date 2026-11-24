namespace VCare.SharedKernel.Abstractions
{
    /// <summary>
    /// Lets one instance of the API at a time run a background job, across every
    /// instance that shares the database.
    /// </summary>
    public interface IJobLock
    {
        /// <summary>
        /// Takes the named lock without waiting. Returns a handle that holds it until
        /// disposed, or null when another instance holds it now.
        /// </summary>
        Task<IAsyncDisposable?> TryAcquireAsync(string name, CancellationToken cancellationToken);
    }
}
