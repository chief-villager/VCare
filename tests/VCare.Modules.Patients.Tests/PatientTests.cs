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
}
