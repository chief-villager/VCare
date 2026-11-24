using VCare.Api.Services;
using Xunit;

namespace VCare.IntegrationTests;

// Several instances of the API share one database; the lock decides which one
// runs a background job's pass.
[Collection(SqlServerCollection.Name)]
public class JobLockTests(SqlServerFixture sql)
{
    [Fact]
    public async Task Only_one_instance_holds_a_lock_until_it_lets_go()
    {
        var first = new SqlServerJobLock(sql.ConnectionString);
        var second = new SqlServerJobLock(sql.ConnectionString);

        var held = await first.TryAcquireAsync("tests.one-holder", CancellationToken.None);
        Assert.NotNull(held);
        Assert.Null(await second.TryAcquireAsync("tests.one-holder", CancellationToken.None));

        await held.DisposeAsync();
        await using var taken = await second.TryAcquireAsync("tests.one-holder", CancellationToken.None);
        Assert.NotNull(taken);
    }

    [Fact]
    public async Task Locks_with_different_names_do_not_block_each_other()
    {
        var jobs = new SqlServerJobLock(sql.ConnectionString);

        await using var one = await jobs.TryAcquireAsync("tests.job-a", CancellationToken.None);
        await using var two = await jobs.TryAcquireAsync("tests.job-b", CancellationToken.None);

        Assert.NotNull(one);
        Assert.NotNull(two);
    }

    // The migration list must name every module's DbContext, or a new module's
    // tables would be missing from these tests and from scripts/migrations.sh.
    [Fact]
    public void Every_module_database_is_migrated()
    {
        Assert.Equal(SqlServerFixture.MigrationOrder.Order(), sql.Api.ContextTypes().Keys.Order());
    }
}
