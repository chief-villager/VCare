using System.Linq.Expressions;
using Medications.Application.Abstracts;
using Medications.Application.Services;
using Medications.Domain.Entities;
using Medications.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using VCare.SharedKernel.Abstractions;

namespace VCare.Modules.Medications.Tests;

/// <summary>A clock set by the test. Local time is UTC, so local "now" is exactly <see cref="Now"/>.</summary>
internal sealed class FixedClock(DateTime now) : TimeProvider
{
    public DateTime Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(Now, DateTimeKind.Utc));
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}

internal sealed class FakeCurrentUser(Guid careHomeId, Guid userId) : ICurrentUser
{
    public Guid UserId { get; } = userId;
    public Guid CareHomeId { get; } = careHomeId;
    public bool IsAuthenticated => true;
    public bool IsInRole(string role) => true;
}

internal sealed class FakePatients(params Guid[] known) : IPatientDirectory
{
    public Task<bool> ExistsAsync(Guid patientId, CancellationToken cancellationToken) =>
        Task.FromResult(known.Contains(patientId));
}

internal sealed class FakeOrders : IMedicationOrderRepository
{
    public List<MedicationOrder> Orders { get; } = [];

    public Task<MedicationOrder?> GetAsync(Guid Id, CancellationToken cancellationToken) =>
        Task.FromResult(Orders.FirstOrDefault(o => o.Id.Value == Id));

    public Task UpdateAsync(MedicationOrder medicationOrder, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task AddAsync(MedicationOrder medicationOrder, CancellationToken cancellationToken)
    {
        Orders.Add(medicationOrder);
        return Task.CompletedTask;
    }

    public Task<List<MedicationOrder>> GetAllMedicationBelongingToAUserAsync(Expression<Func<MedicationOrder, bool>> expression, CancellationToken cancellationToken) =>
        Task.FromResult(Orders.AsQueryable().Where(expression).ToList());

    public Task<IEnumerable<MedicationOrder>> ActiveBetween(Guid patientId, DateOnly from, DateOnly to) =>
        Task.FromResult<IEnumerable<MedicationOrder>>(Orders
            .Where(o => o.PatientId.Value == patientId && o.StartDate <= to && (o.EndDate == null || o.EndDate >= from))
            .ToList());

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

    public Task<List<MedicationOrder>> ActiveScheduledForAllCareHomesAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        Task.FromResult(Orders
            .Where(o => o.Status == OrderStatus.Active && !o.IsPrn && o.StartDate <= to && (o.EndDate == null || o.EndDate >= from))
            .ToList());
}

/// <summary>Administrations held in memory. Saving enforces the database's one
/// record per (order, slot) rule, as the unique index does.</summary>
internal sealed class FakeAdministrations : IMedicalAdministrationRepository
{
    private readonly List<MedicationAdministration> _pending = [];
    public List<MedicationAdministration> Saved { get; } = [];

    public Task AddAsync(MedicationAdministration administration)
    {
        _pending.Add(administration);
        return Task.CompletedTask;
    }

    public Task<MedicationAdministration?> Find(Guid orderId, DateTime scheduledFor) =>
        Task.FromResult(Saved.FirstOrDefault(a => a.MedicationOrderId.Value == orderId && a.ScheduledFor == scheduledFor));

    public Task<List<MedicationAdministration>> ForResidentBetweenAsync(Guid patientId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var start = from.ToDateTime(TimeOnly.MinValue);
        var end = to.ToDateTime(TimeOnly.MaxValue);
        return Task.FromResult(Saved
            .Where(a => a.PatientId.Value == patientId
                     && ((a.ScheduledFor != null && a.ScheduledFor >= start && a.ScheduledFor <= end)
                      || (a.ScheduledFor == null && a.AdministeredAt >= start && a.AdministeredAt <= end)))
            .ToList());
    }

    public Task<List<DateTime>> PrnGivenTimesAsync(Guid orderId, DateTime from, DateTime to, CancellationToken cancellationToken) =>
        Task.FromResult(Saved
            .Where(a => a.MedicationOrderId.Value == orderId && a.ScheduledFor == null
                     && a.OutcomeCodeId == OutcomeCode.TakenId
                     && a.AdministeredAt >= from && a.AdministeredAt <= to)
            .Select(a => a.AdministeredAt!.Value)
            .ToList());

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var a in _pending.Where(p => p.ScheduledFor != null))
        {
            if (Saved.Any(s => s.MedicationOrderId == a.MedicationOrderId && s.ScheduledFor == a.ScheduledFor))
                throw new DbUpdateException("Unique index IX_MedicationAdminstration_MedicationOrderId_ScheduledFor violated.");
        }
        Saved.AddRange(_pending);
        var count = _pending.Count;
        _pending.Clear();
        return Task.FromResult(count);
    }

    public Task<HashSet<(Guid OrderId, DateTime ScheduledFor)>> RecordedSlotsForAllCareHomesAsync(
        IReadOnlyCollection<Guid> orderIds, DateTime from, DateTime to, CancellationToken cancellationToken) =>
        Task.FromResult(Saved
            .Where(a => orderIds.Contains(a.MedicationOrderId.Value) && a.ScheduledFor >= from && a.ScheduledFor <= to)
            .Select(a => (a.MedicationOrderId.Value, a.ScheduledFor!.Value))
            .ToHashSet());

    public void DiscardPendingChanges() => _pending.Clear();
}

internal sealed class FakeOutcomes : IOutcomeRepository
{
    public Task<OutcomeCode?> GetOutcomeByName(string name, CancellationToken cancellationToken) =>
        Task.FromResult(OutcomeCode.Seeded.FirstOrDefault(o => string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<OutcomeCode>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult(OutcomeCode.Seeded);
}

/// <summary>One home, one resident, and the services wired the way the module wires them.</summary>
internal sealed class MedicationWorld
{
    public static readonly Guid CareHome = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid Resident = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    public static readonly Guid OtherResident = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    public static readonly Guid Carer = Guid.Parse("cccccccc-0000-0000-0000-000000000001");

    public FixedClock Clock { get; }
    public FakeOrders Orders { get; } = new();
    public FakeAdministrations Administrations { get; } = new();
    public FakeOutcomes Outcomes { get; } = new();
    public ScheduleExpander Expander { get; } = new();

    public MedicationWorld(DateTime now) => Clock = new FixedClock(now);

    public MedicationService Medications => new(Orders, Administrations, Expander, Outcomes,
        new FakeCurrentUser(CareHome, Carer), new FakePatients(Resident, OtherResident), Clock);

    public MarChartService Chart => new(Orders, Administrations, Expander, Outcomes, Clock);

    public MissedDoseRecorder Recorder => new(Orders, Administrations, Expander,
        Microsoft.Extensions.Logging.Abstractions.NullLogger<MissedDoseRecorder>.Instance);

    /// <summary>A regular order given every day at the times named, entered at <paramref name="createdAt"/>.</summary>
    public MedicationOrder AddDailyOrder(DateOnly start, DateTime createdAt, params TimeOnly[] times)
    {
        var order = MedicationOrder.Record(Resident, CareHome, "Paracetamol 500mg", MedicationRouteEnum.ORAL, null,
            start, null, "Dr Okafor", false, null, null, null, false, OrderStatus.Active,
            [("1 tablet", FrequencyType.DailyAtTimes, times.ToList(), 1, DayOfWeekFlags.None, null, start, null, 1)],
            createdAt).Value;
        Orders.Orders.Add(order);
        return order;
    }

    /// <summary>An as-required order with the given minimum interval and 24-hour dose limit.</summary>
    public MedicationOrder AddPrnOrder(DateOnly start, DateTime createdAt, int? minIntervalMinutes, int? maxDosesPer24h)
    {
        var order = MedicationOrder.Record(Resident, CareHome, "Paracetamol 500mg", MedicationRouteEnum.ORAL, null,
            start, null, "Dr Okafor", true, "Pain", minIntervalMinutes, "8 tablets in 24 hours", false, OrderStatus.Active,
            [], createdAt, prnMaxDosesPer24h: maxDosesPer24h).Value;
        Orders.Orders.Add(order);
        return order;
    }

    public Guid ScheduleIdOf(MedicationOrder order) => order.Schedule.Single().Id;
}
