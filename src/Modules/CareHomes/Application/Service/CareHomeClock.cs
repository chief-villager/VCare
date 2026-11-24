using src.Modules.CareHomes.Application.Contract;
using VCare.SharedKernel.Abstractions;

namespace src.Modules.CareHomes.Application.Service
{
    /// <summary>
    /// The wall clock in a care home: the server's UTC time read in the home's zone.
    /// </summary>
    internal sealed class CareHomeClock(ICareHomeRepository careHomes, TimeProvider clock) : ICareHomeClock
    {
        public async Task<DateTime> NowAsync(Guid careHomeId, CancellationToken cancellationToken)
        {
            var home = await careHomes.GetByIdAsync(new CareHomeId(careHomeId), cancellationToken)
                ?? throw new InvalidOperationException($"Care home {careHomeId} does not exist, so it has no clock.");
            var zone = TimeZoneInfo.FindSystemTimeZoneById(home.TimeZoneId);
            // Unspecified, not Local or Utc: it is a time on the home's wall, which is
            // how dose times are stored and compared.
            return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(clock.GetUtcNow().UtcDateTime, zone), DateTimeKind.Unspecified);
        }
    }
}
