using System;
using System.Linq;
using VCare.Modules.Patients.Domain.Entities;
using VCare.Modules.Patients.Domain.Events;
using VCare.SharedKernel.Results;
using Xunit;

namespace VCare.Modules.Patients.Tests;

public class PatientTests
{
    private static Result<Patient> RegisterAda() =>
        Patient.Register(
            firstname: "Ada",
            lastname: "Lovelace",
            gender: "Female",
            address: "1 Analytical Way",
            dateOfBirth: new DateOnly(1990, 5, 12),
            emergencyContactRelationship: "Spouse",
            emergencyContactPhoneNumber: "08000000000",
            emergencyContactName: "Charles Babbage",
            phoneNumber: null,
            careHomeId: Guid.NewGuid(),
            email: null);

    [Fact]
    public void Register_assigns_an_id_and_raises_a_domain_event()
    {
        var result = RegisterAda();

        Assert.True(result.IsSuccess);
        var patient = result.Value;
        Assert.NotEqual(Guid.Empty, patient.Id.Value);
        Assert.Equal("Ada Lovelace", patient.FullName);
        Assert.Single(patient.DomainEvents);
        Assert.IsType<PatientRegistered>(patient.DomainEvents[0]);
    }

    [Fact]
    public void Register_fails_when_firstname_is_missing()
    {
        var result = Patient.Register(
            firstname: " ",
            lastname: "Lovelace",
            gender: "Female",
            address: "1 Analytical Way",
            dateOfBirth: new DateOnly(1990, 5, 12),
            emergencyContactRelationship: "Spouse",
            emergencyContactPhoneNumber: "08000000000",
            emergencyContactName: "Charles Babbage",
            phoneNumber: null,
            careHomeId: Guid.NewGuid(),
            email: null);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Update_changes_the_supplied_fields()
    {
        var patient = RegisterAda().Value;

        patient.Update(firstname: "Augusta", lastname: "King");

        Assert.Equal("Augusta King", patient.FullName);
    }

    [Fact]
    public void AddCarePlan_adds_a_care_plan_to_the_patient_aggregate()
    {
        var patient = RegisterAda().Value;

        var result = patient.AddCarePlan(
            staffId: Guid.NewGuid(),
            diagnoses: ["Hypertension"],
            goals: ["Lower blood pressure"],
            interventions: [("Daily monitoring", false)]);

        Assert.True(result.IsSuccess);
        var carePlan = Assert.Single(patient.CarePlans);
        Assert.Equal(patient.Id, carePlan.PatientId);
        Assert.Equal("Hypertension", Assert.Single(carePlan.Diagnoses).Description);
    }

    [Fact]
    public void UpdateCarePlan_replaces_details_of_an_existing_care_plan()
    {
        var patient = RegisterAda().Value;
        var carePlan = patient.AddCarePlan(staffId: Guid.NewGuid(), diagnoses: ["Initial"]).Value;

        var result = patient.UpdateCarePlan(carePlan.Id, diagnoses: ["Revised"]);

        Assert.True(result.IsSuccess);
        Assert.Equal("Revised", patient.CarePlans.Single().Diagnoses.Single().Description);
    }

    [Fact]
    public void UpdateCarePlan_fails_for_an_unknown_care_plan()
    {
        var patient = RegisterAda().Value;

        var result = patient.UpdateCarePlan(VCare.SharedKernel.Abstractions.CarePlanId.New());

        Assert.True(result.IsFailure);
    }
}
