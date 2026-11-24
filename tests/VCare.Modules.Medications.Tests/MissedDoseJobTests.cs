using Medications.Application.Services;
using Microsoft.Extensions.Logging.Abstractions;
using VCare.SharedKernel.Abstractions;
using Xunit;

namespace VCare.Modules.Medications.Tests;

// Every instance of the API runs the job's timer. Before the lock, each one
// repeated the same pass over every care home.
public class MissedDoseJobTests
{
    private static DateTime At(int day, int hour, int minute = 0) => new(2026, 11, day, hour, minute, 0);

    private static MedicationWorld WorldWithAnOverdueDose()
    {
        var world = new MedicationWorld(At(19, 9, 5));
        world.AddDailyOrder(new DateOnly(2026, 11, 19), At(19, 7, 0), new TimeOnly(8, 0));
        return world;
    }

    [Fact]
    public async Task A_pass_is_skipped_while_another_instance_holds_the_lock()
    {
        var world = WorldWithAnOverdueDose();
        var jobLock = new FakeJobLock(available: false);

        var recorded = await new MissedDoseJob(jobLock, world.Recorder, NullLogger<MissedDoseJob>.Instance)
            .RunOnceAsync(CancellationToken.None);

        Assert.Null(recorded);
        Assert.Empty(world.Administrations.Saved);
        Assert.Equal([MissedDoseJob.LockName], jobLock.Requested);
    }

    [Fact]
    public async Task The_instance_holding_the_lock_runs_the_pass_and_then_releases_it()
    {
        var world = WorldWithAnOverdueDose();
        var jobLock = new FakeJobLock(available: true);

        var recorded = await new MissedDoseJob(jobLock, world.Recorder, NullLogger<MissedDoseJob>.Instance)
            .RunOnceAsync(CancellationToken.None);

        Assert.Equal(1, recorded);
        Assert.Single(world.Administrations.Saved);
        Assert.Equal(1, jobLock.Released);
    }

    private sealed class FakeJobLock(bool available) : IJobLock
    {
        public List<string> Requested { get; } = [];
        public int Released { get; private set; }

        public Task<IAsyncDisposable?> TryAcquireAsync(string name, CancellationToken cancellationToken)
        {
            Requested.Add(name);
            return Task.FromResult<IAsyncDisposable?>(available ? new Handle(this) : null);
        }

        private sealed class Handle(FakeJobLock owner) : IAsyncDisposable
        {
            public ValueTask DisposeAsync()
            {
                owner.Released++;
                return ValueTask.CompletedTask;
            }
        }
    }
}

// A pass shows up in traces with what it did, so a quiet night on the MAR can
// be told apart from a job that never ran.
public class MissedDoseJobTracingTests
{
    private const string TestSourceName = "VCare.Tests.MissedDoseJobTracing";
    private static readonly System.Diagnostics.ActivitySource TestSource = new(TestSourceName);

    [Fact]
    public async Task A_pass_is_traced_with_the_doses_it_recorded()
    {
        var stopped = new List<System.Diagnostics.Activity>();
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = source => source.Name is MissedDoseJob.TraceSourceName or TestSourceName,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) =>
                System.Diagnostics.ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => { lock (stopped) { stopped.Add(activity); } },
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);
        var world = new MedicationWorld(new DateTime(2026, 11, 19, 9, 5, 0));
        world.AddDailyOrder(new DateOnly(2026, 11, 19), new DateTime(2026, 11, 19, 7, 0, 0), new TimeOnly(8, 0));

        // Other tests run jobs in parallel; only this test's trace counts.
        using var test = TestSource.StartActivity("test")!;
        await new MissedDoseJob(new AlwaysFree(), world.Recorder, NullLogger<MissedDoseJob>.Instance)
            .RunOnceAsync(CancellationToken.None);

        System.Diagnostics.Activity pass;
        lock (stopped)
        {
            pass = Assert.Single(stopped, a => a.TraceId == test.TraceId && a.Source.Name == MissedDoseJob.TraceSourceName);
        }
        Assert.Equal("Record missed doses", pass.DisplayName);
        Assert.Equal(1, pass.GetTagItem("vcare.missed_doses.recorded"));
    }

    private sealed class AlwaysFree : IJobLock
    {
        public Task<IAsyncDisposable?> TryAcquireAsync(string name, CancellationToken cancellationToken) =>
            Task.FromResult<IAsyncDisposable?>(new Nothing());

        private sealed class Nothing : IAsyncDisposable
        {
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
