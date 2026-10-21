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
            goal: "Lower blood pressure",
            staffId: Guid.NewGuid(),
            careHomeId: CareHomeId,
            diagnoses: diagnoses.Length == 0 ? null : diagnoses.Select(d => (d, (string?)null, (string?)null))).Value;

    [Fact]
    public void Create_assigns_an_id_and_keeps_the_patient_and_care_home()
    {
        var result = CarePlan.Create(
            patientId: PatientId,
            goal: "Lower blood pressure",
            staffId: Guid.NewGuid(),
            careHomeId: CareHomeId,
            diagnoses: [("Hypertension", "Below 140/90", "Daily monitoring")]);

        Assert.True(result.IsSuccess);
        var carePlan = result.Value;
        Assert.NotEqual(Guid.Empty, carePlan.Id.Value);
        Assert.Equal(PatientId, carePlan.PatientId.Value);
        Assert.Equal(CareHomeId, carePlan.CareHomeId.Value);
        Assert.Equal("Lower blood pressure", carePlan.Goal);
        var diagnosis = Assert.Single(carePlan.Diagnoses);
        Assert.Equal("Hypertension", diagnosis.Name);
        Assert.Equal("Below 140/90", diagnosis.GoalDescription);
        Assert.Equal("Daily monitoring", diagnosis.Intervention);
    }

    [Fact]
    public void Update_replaces_details_of_an_existing_care_plan()
    {
        var carePlan = CreatePlan("Initial");

        var result = carePlan.Update(diagnoses: [("Revised", null, "Weekly review")]);

        Assert.True(result.IsSuccess);
        var diagnosis = carePlan.Diagnoses.Single();
        Assert.Equal("Revised", diagnosis.Name);
        Assert.Equal("Weekly review", diagnosis.Intervention);
    }

    [Fact]
    public void Create_fails_when_the_patient_is_missing()
    {
        var result = CarePlan.Create(
            patientId: Guid.Empty,
            goal: "Lower blood pressure",
            staffId: Guid.NewGuid(),
            careHomeId: CareHomeId);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Create_fails_when_the_care_home_is_missing()
    {
        var result = CarePlan.Create(
            patientId: PatientId,
            goal: "Lower blood pressure",
            staffId: Guid.NewGuid(),
            careHomeId: Guid.Empty);

        Assert.True(result.IsFailure);
    }
}
