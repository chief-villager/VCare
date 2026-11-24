using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VCare.Modules.CarePlans.Application.Abstractions;
using VCare.Modules.CarePlans.Application.Services;
using VCare.Modules.CarePlans.Domain.Entities;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Results;
using Xunit;

namespace VCare.Modules.CarePlans.Tests;

public class CarePlanServiceTests
{
    private static readonly Guid CareHome = Guid.NewGuid();
    private static readonly Guid Resident = Guid.NewGuid();

    private sealed class FakePlans : ICarePlanRepository
    {
        public List<CarePlan> Plans { get; } = [];
        public Task<CarePlan?> GetByIdAsync(CarePlanId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Plans.FirstOrDefault(p => p.Id == id));
        public Task<CarePlan?> GetForPatientAsync(PatientId patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Plans.FirstOrDefault(p => p.PatientId == patientId));
        public Task<bool> ExistsForPatientAsync(PatientId patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Plans.Any(p => p.PatientId == patientId));
        public Task AddAsync(CarePlan carePlan, CancellationToken cancellationToken = default)
        {
            Plans.Add(carePlan);
            return Task.CompletedTask;
        }
        public bool LoseTheRace { get; set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            LoseTheRace ? throw new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException("Another save won.") : Task.FromResult(1);
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public Guid CareHomeId => CareHome;
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => true;
    }

    private sealed class FakePatients(params Guid[] known) : IPatientDirectory
    {
        public Task<bool> ExistsAsync(Guid patientId, CancellationToken cancellationToken) =>
            Task.FromResult(known.Contains(patientId));
    }

    [Fact]
    public async Task A_care_plan_cannot_be_created_for_a_resident_who_is_not_in_the_home()
    {
        var plans = new FakePlans();
        var service = new CarePlanService(plans, new FakeCurrentUser(), new FakePatients(Resident));

        var result = await service.CreateCarePlanAsync(Guid.NewGuid(),
            new CreateCarePlanRequest("Stay mobile", null), CancellationToken.None);

        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
        Assert.Empty(plans.Plans);
    }

    [Fact]
    public async Task A_care_plan_is_created_for_a_resident_of_the_home()
    {
        var plans = new FakePlans();
        var service = new CarePlanService(plans, new FakeCurrentUser(), new FakePatients(Resident));

        var result = await service.CreateCarePlanAsync(Resident,
            new CreateCarePlanRequest("Stay mobile", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Resident, Assert.Single(plans.Plans).PatientId.Value);
    }

    [Fact]
    public async Task The_author_of_a_care_plan_is_whoever_is_signed_in()
    {
        var plans = new FakePlans();
        var caller = new FakeCurrentUser();
        var service = new CarePlanService(plans, caller, new FakePatients(Resident));

        await service.CreateCarePlanAsync(Resident, new CreateCarePlanRequest("Stay mobile", null), CancellationToken.None);

        Assert.Equal(caller.UserId, Assert.Single(plans.Plans).StaffId);
    }

    private static (FakePlans Plans, CarePlanService Service, CarePlan Plan) WithAPlan()
    {
        var plans = new FakePlans();
        var plan = CarePlan.Create(Resident, "Stay mobile", Guid.NewGuid(), CareHome, null).Value;
        plans.Plans.Add(plan);
        return (plans, new CarePlanService(plans, new FakeCurrentUser(), new FakePatients(Resident)), plan);
    }

    [Fact]
    public async Task An_edit_based_on_an_old_version_is_a_conflict()
    {
        var (_, service, plan) = WithAPlan();
        await service.UpdateCarePlanAsync(Resident, plan.Id.Value, new UpdateCarePlanRequest(null, "First edit", 1), CancellationToken.None);

        var result = await service.UpdateCarePlanAsync(Resident, plan.Id.Value, new UpdateCarePlanRequest(null, "Second edit", 1), CancellationToken.None);

        Assert.Equal(ErrorKind.Conflict, result.Error.Kind);
        Assert.Equal("CarePlans.EditConflict", result.Error.Code);
    }

    [Fact]
    public async Task An_edit_that_loses_a_race_to_another_save_is_a_conflict()
    {
        var (plans, service, plan) = WithAPlan();
        plans.LoseTheRace = true;

        var result = await service.UpdateCarePlanAsync(Resident, plan.Id.Value, new UpdateCarePlanRequest(null, "Edit", 1), CancellationToken.None);

        Assert.Equal("CarePlans.EditConflict", result.Error.Code);
    }

    [Fact]
    public async Task The_versions_of_a_care_plan_are_listed_oldest_first()
    {
        var (_, service, plan) = WithAPlan();
        await service.UpdateCarePlanAsync(Resident, plan.Id.Value, new UpdateCarePlanRequest(null, "Second", 1), CancellationToken.None);
        await service.UpdateCarePlanAsync(Resident, plan.Id.Value, new UpdateCarePlanRequest(null, "Third", 2), CancellationToken.None);

        var versions = await service.GetVersionsAsync(Resident, CancellationToken.None);
        var third = await service.GetVersionAsync(Resident, 3, CancellationToken.None);

        Assert.Equal([1, 2, 3], versions.Value.Select(v => v.Number));
        Assert.Equal("Third", third.Value.Goal);
    }
}
