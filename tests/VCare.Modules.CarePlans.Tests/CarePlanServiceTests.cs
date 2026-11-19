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
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
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
            new CreateCarePlanRequest(Guid.NewGuid(), "Stay mobile", null), CancellationToken.None);

        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
        Assert.Empty(plans.Plans);
    }

    [Fact]
    public async Task A_care_plan_is_created_for_a_resident_of_the_home()
    {
        var plans = new FakePlans();
        var service = new CarePlanService(plans, new FakeCurrentUser(), new FakePatients(Resident));

        var result = await service.CreateCarePlanAsync(Resident,
            new CreateCarePlanRequest(Guid.NewGuid(), "Stay mobile", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Resident, Assert.Single(plans.Plans).PatientId.Value);
    }
}
