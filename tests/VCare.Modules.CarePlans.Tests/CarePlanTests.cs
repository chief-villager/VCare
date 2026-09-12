using System;
using System.Linq;
using VCare.Modules.CarePlans.Domain.Entities;
using Xunit;

namespace VCare.Modules.CarePlans.Tests;

public class CarePlanTests
{
    private static readonly Guid PatientId = Guid.NewGuid();
    private static readonly Guid CareHomeId = Guid.NewGuid();

    private static CarePlan CreatePlan(params string[] diagnoses) =>
        CarePlan.Create(
            patientId: PatientId,
            staffId: Guid.NewGuid(),
            careHomeId: CareHomeId,
            diagnoses: diagnoses.Length == 0 ? null : diagnoses,
            goals: ["Lower blood pressure"],
            interventions: [("Daily monitoring", false)]).Value;

    [Fact]
    public void Create_assigns_an_id_and_keeps_the_patient_and_care_home()
    {
        var result = CarePlan.Create(
            patientId: PatientId,
            staffId: Guid.NewGuid(),
            careHomeId: CareHomeId,
            diagnoses: ["Hypertension"],
            goals: ["Lower blood pressure"],
            interventions: [("Daily monitoring", false)]);

        Assert.True(result.IsSuccess);
        var carePlan = result.Value;
        Assert.NotEqual(Guid.Empty, carePlan.Id.Value);
        Assert.Equal(PatientId, carePlan.PatientId.Value);
        Assert.Equal(CareHomeId, carePlan.CareHomeId.Value);
        Assert.Equal("Hypertension", Assert.Single(carePlan.Diagnoses).Description);
    }

    [Fact]
    public void Update_replaces_details_of_an_existing_care_plan()
    {
        var carePlan = CreatePlan("Initial");

        var result = carePlan.Update(diagnoses: ["Revised"]);

        Assert.True(result.IsSuccess);
        Assert.Equal("Revised", carePlan.Diagnoses.Single().Description);
    }

    [Fact]
    public void Create_fails_when_the_patient_is_missing()
    {
        var result = CarePlan.Create(
            patientId: Guid.Empty,
            staffId: Guid.NewGuid(),
            careHomeId: CareHomeId);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_fails_when_the_care_home_is_missing()
    {
        var result = CarePlan.Create(
            patientId: PatientId,
            staffId: Guid.NewGuid(),
            careHomeId: Guid.Empty);

        Assert.True(result.IsFailure);
    }
}
