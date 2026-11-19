using VCare.Modules.Visitation.Application.Abstractions;
using VCare.Modules.Visitation.Application.Services;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;
using Visitation.Domain.Entities;
using Xunit;

namespace VCare.Modules.Visitation.Tests;

public class VisitationServiceTests
{
    private static readonly Guid CareHome = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Resident = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    private static readonly Guid OtherResident = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly Guid Carer = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
    private static readonly DateOnly Day = new(2026, 11, 19);

    private sealed class FakeVisits : IVisitRepository
    {
        public List<Visit> Visits { get; } = [];
        public Task<Visit?> GetByIdAsync(VisitId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Visits.FirstOrDefault(v => v.Id == id));
        public Task<IReadOnlyList<Visit>> GetForPatientAsync(PatientId patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Visit>>(Visits.Where(v => v.PatientId == patientId).ToList());
        public Task AddRangeAsync(IEnumerable<Visit> visits, CancellationToken cancellationToken = default)
        {
            Visits.AddRange(visits);
            return Task.CompletedTask;
        }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid UserId => Carer;
        public Guid CareHomeId => CareHome;
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => true;
    }

    private sealed class Known(params Guid[] ids) : IPatientDirectory, IStaffDirectory
    {
        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(ids.Contains(id));
    }

    private static (VisitationService Service, FakeVisits Visits, Visit Visit) HomeWithOneVisit()
    {
        var visits = new FakeVisits();
        var visit = Visit.CreateVisit(new PatientId(Resident), new CareHomeId(CareHome), [Day], new TimeOnly(9, 0)).Value.Single();
        visit.AssignVisit(new StaffId(Carer));
        visits.Visits.Add(visit);
        var known = new Known(Resident, OtherResident, Carer);
        return (new VisitationService(visits, new FakeCurrentUser(), known, known), visits, visit);
    }

    [Fact]
    public async Task Visits_cannot_be_booked_for_a_resident_who_is_not_in_the_home()
    {
        var (service, visits, _) = HomeWithOneVisit();

        var result = await service.ScheduleVisitsAsync(Guid.NewGuid(), new ScheduleVisitRequest([Day], new TimeOnly(10, 0)), CancellationToken.None);

        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
        Assert.Single(visits.Visits);
    }

    [Fact]
    public async Task A_visit_cannot_be_assigned_to_someone_who_is_not_on_the_homes_staff()
    {
        // Nobody could ever check in: the visit would silently never happen.
        var (service, _, visit) = HomeWithOneVisit();

        var result = await service.AssignStaffAsync(Resident, visit.Id.Value, new AssignStaffRequest(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
        Assert.Equal([new StaffId(Carer)], visit.StaffIds);
    }

    [Fact]
    public async Task A_carer_cannot_check_in_through_another_residents_url()
    {
        var (service, _, visit) = HomeWithOneVisit();

        var result = await service.CheckInAsync(OtherResident, visit.Id.Value,
            new CheckInVisitRequest(Day.ToDateTime(new TimeOnly(9, 0))), CancellationToken.None);

        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
        Assert.Empty(visit.VisitRecords);
    }

    [Fact]
    public async Task A_visit_cannot_be_cancelled_through_another_residents_url()
    {
        var (service, _, visit) = HomeWithOneVisit();

        var result = await service.CancelVisitAsync(OtherResident, visit.Id.Value, new CancelVisitRequest("Unwell"), CancellationToken.None);

        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
        Assert.False(visit.IsCancelled);
    }

    [Fact]
    public async Task A_visit_cannot_be_read_through_another_residents_url()
    {
        var (service, _, visit) = HomeWithOneVisit();

        var result = await service.GetByIdAsync(OtherResident, visit.Id.Value, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task A_carer_checks_in_through_the_residents_own_url()
    {
        var (service, _, visit) = HomeWithOneVisit();

        var result = await service.CheckInAsync(Resident, visit.Id.Value,
            new CheckInVisitRequest(Day.ToDateTime(new TimeOnly(9, 0))), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(visit.VisitRecords);
    }
}
