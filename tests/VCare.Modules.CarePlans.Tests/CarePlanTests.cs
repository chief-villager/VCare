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

        var result = carePlan.Update([("Revised", null, "Weekly review")], null, basedOnVersion: 1, Guid.NewGuid(), DateTime.UtcNow);

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

    // Care plans are edited by several people. Each edit is a version, the old
    // ones are kept, and an edit made to a version someone has since replaced is
    // refused instead of silently overwriting their change.
    [Fact]
    public void A_new_care_plan_is_version_one_and_keeps_a_copy_of_itself()
    {
        var plan = CreatePlan("Falls risk");

        Assert.Equal(1, plan.Version);
        var first = Assert.Single(plan.Versions);
        Assert.Equal(1, first.Number);
        Assert.Equal("Lower blood pressure", first.Goal);
        Assert.Contains("Falls risk", first.Diagnoses);
    }

    [Fact]
    public void Each_edit_makes_a_new_version_and_keeps_the_old_one()
    {
        var plan = CreatePlan("Falls risk");
        var nurse = Guid.NewGuid();
        var at = new DateTime(2026, 11, 22, 10, 0, 0);

        var result = plan.Update(null, "Walk to the dining room daily", basedOnVersion: 1, nurse, at);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, plan.Version);
        Assert.Equal(["Lower blood pressure", "Walk to the dining room daily"], plan.Versions.OrderBy(v => v.Number).Select(v => v.Goal));
        var second = plan.Versions.Single(v => v.Number == 2);
        Assert.Equal(nurse, second.EditedByStaffId);
        Assert.Equal(at, second.EditedAt);
    }

    [Fact]
    public void An_edit_to_a_version_someone_has_replaced_is_refused_and_changes_nothing()
    {
        var plan = CreatePlan("Falls risk");
        plan.Update(null, "First nurse's goal", basedOnVersion: 1, Guid.NewGuid(), DateTime.UtcNow);

        var second = plan.Update(null, "Second nurse's goal", basedOnVersion: 1, Guid.NewGuid(), DateTime.UtcNow);

        Assert.Equal("CarePlans.EditConflict", second.Error.Code);
        Assert.Equal("First nurse's goal", plan.Goal);
        Assert.Equal(2, plan.Version);
    }
}
