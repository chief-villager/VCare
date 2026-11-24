using System.Diagnostics;
using Microsoft.Extensions.Logging;
using VCare.SharedKernel.Abstractions;

namespace Medications.Application.Services
{
    /// <summary>
    /// One pass of the missed-dose job. Every instance of the API runs the timer,
    /// but only the one holding the lock does the pass; the others skip it.
    /// </summary>
    internal sealed class MissedDoseJob(IJobLock jobLock, MissedDoseRecorder recorder, ILogger<MissedDoseJob> logger)
    {
        internal const string LockName = "VCare.Medications.MissedDoses";
        internal const string TraceSourceName = "VCare.Medications";
        private static readonly ActivitySource Trace = new(TraceSourceName);

        /// <summary>The doses recorded, or null when another instance is running the pass.</summary>
        public async Task<int?> RunOnceAsync(CancellationToken cancellationToken)
        {
            using var activity = Trace.StartActivity("Record missed doses");
            await using var held = await jobLock.TryAcquireAsync(LockName, cancellationToken);
            if (held is null)
            {
                activity?.SetTag("vcare.missed_doses.skipped", true);
                logger.LogDebug("Another instance is recording missed doses; skipping this pass.");
                return null;
            }
            var recorded = await recorder.RecordMissedDosesAsync(cancellationToken);
            activity?.SetTag("vcare.missed_doses.recorded", recorded);
            return recorded;
        }
    }
}
