using VCare.Modules.Patients.Domain.Entities;
using VCare.Modules.Patients.Domain.Events;
using Xunit;

namespace VCare.Modules.Patients.Tests;

public class PatientTests
{
    [Fact]
    public void Register_assigns_an_id_and_raises_a_domain_event()
    {
        var patient = Patient.Register("Ada Lovelace", new DateOnly(1990, 5, 12));

        Assert.NotEqual(Guid.Empty, patient.Id);
        Assert.Equal("Ada Lovelace", patient.FullName);
        Assert.Single(patient.DomainEvents);
        Assert.IsType<PatientRegistered>(patient.DomainEvents[0]);
    }

    [Fact]
    public void Rename_rejects_empty_names()
    {
        var patient = Patient.Register("Ada Lovelace", new DateOnly(1990, 5, 12));

        Assert.Throws<ArgumentException>(() => patient.Rename(" "));
    }
}
