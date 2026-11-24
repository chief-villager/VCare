using src.Modules.CareHomes.Application.Contract;
using src.Modules.CareHomes.Application.Service;
using src.Modules.CareHomes.Domain.Entity;
using VCare.SharedKernel.Abstractions;
using Xunit;

namespace CareHomes.Tests;

public class CareHomeTimeZoneTests
{
    private sealed class FixedClock(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));
    }

    private sealed class Homes(params CareHome[] homes) : ICareHomeRepository
    {
        public Task AddAsync(CareHome careHome, CancellationToken cancellationToken) => Task.CompletedTask;
        public void UpdateAsync(CareHome careHome) { }
        public void Delete(CareHome careHome) { }
        public Task<CareHome?> GetByIdAsync(CareHomeId id, CancellationToken cancellationToken) =>
            Task.FromResult(homes.FirstOrDefault(h => h.Id == id));
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private static CareHome Home(string zone) =>
        CareHome.CreateCareHome("Oak House", "1 High Street", "office@oak.example", zone).Value;

    [Fact]
    public void A_care_home_needs_a_time_zone_the_server_recognises()
    {
        var missing = CareHome.CreateCareHome("Oak House", "1 High Street", "office@oak.example", "");
        var unknown = CareHome.CreateCareHome("Oak House", "1 High Street", "office@oak.example", "Mars/Olympus_Mons");

        Assert.Equal("CareHomes.InvalidTimeZone", missing.Error.Code);
        Assert.Equal("CareHomes.InvalidTimeZone", unknown.Error.Code);
    }

    [Fact]
    public void An_unrecognised_time_zone_is_refused_on_update_and_the_old_one_kept()
    {
        var home = Home("Europe/London");

        var result = home.UpdateCareHome(null, null, null, "Not/AZone");

        Assert.Equal("CareHomes.InvalidTimeZone", result.Error.Code);
        Assert.Equal("Europe/London", home.TimeZoneId);
    }

    [Fact]
    public async Task The_clock_reads_the_time_in_the_care_homes_zone()
    {
        var home = Home("Europe/London");
        var clock = new CareHomeClock(new Homes(home), new FixedClock(new DateTime(2026, 7, 1, 8, 30, 0)));

        var now = await clock.NowAsync(home.Id.Value, CancellationToken.None);

        // British Summer Time: UTC+1.
        Assert.Equal(new DateTime(2026, 7, 1, 9, 30, 0), now);
    }

    [Fact]
    public async Task The_clock_follows_daylight_saving_through_the_year()
    {
        var home = Home("Europe/London");
        var clock = new CareHomeClock(new Homes(home), new FixedClock(new DateTime(2026, 12, 1, 8, 30, 0)));

        Assert.Equal(new DateTime(2026, 12, 1, 8, 30, 0), await clock.NowAsync(home.Id.Value, CancellationToken.None));
    }
}
