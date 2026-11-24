using Medications.Application.Services;
using Medications.Domain.Entities;
using VCare.SharedKernel.Results;
using Xunit;

namespace VCare.Modules.Medications.Tests;

// The outcomes a carer can record, and the second signature a controlled drug needs.
public class OutcomeAndWitnessTests
{
    private static readonly DateOnly Day = new(2026, 11, 19);
    private static readonly DateTime Eight = Day.ToDateTime(new TimeOnly(8, 0));

    private static (MedicationWorld World, MedicationOrder Order) Round(bool controlledDrug = false)
    {
        var world = new MedicationWorld(Eight.AddMinutes(5));
        var order = world.AddDailyOrder(Day, Day.AddDays(-1).ToDateTime(new TimeOnly(18, 0)), controlledDrug, new TimeOnly(8, 0));
        return (world, order);
    }

    private static Task<Result<MedicationAdministrationResponse>> Sign(MedicationWorld world, MedicationOrder order,
        string outcome = "Taken", string? reason = null, Guid? witness = null) =>
        world.Medications.RecordAministration(MedicationWorld.Resident, order.Id.Value, world.ScheduleIdOf(order),
            new RecordAdministrationRequest(Eight, outcome, null, null, reason, witness), MedicationWorld.Carer, CancellationToken.None);

    [Fact]
    public void Carers_can_record_why_a_dose_was_not_given_with_distinct_letters()
    {
        var names = OutcomeCode.Seeded.Select(o => o.Name).ToList();

        foreach (var expected in new[] { "Taken", "Refused", "Missed", "Withheld", "Asleep", "In hospital", "On leave", "Out of stock", "Vomited" })
            Assert.Contains(expected, names);
        Assert.Equal(OutcomeCode.Seeded.Count, OutcomeCode.Seeded.Select(o => o.DisplayLetter).Distinct().Count());
        Assert.Equal(OutcomeCode.Seeded.Count, OutcomeCode.Seeded.Select(o => o.Id).Distinct().Count());
    }

    [Fact]
    public async Task A_withheld_dose_needs_a_reason()
    {
        var (world, order) = Round();

        var result = await Sign(world, order, "Withheld");

        Assert.Equal("Medications.OutcomeReasonRequired", result.Error.Code);
        Assert.Empty(world.Administrations.Saved);
    }

    [Fact]
    public async Task A_withheld_dose_with_a_reason_is_recorded()
    {
        var (world, order) = Round();

        var result = await Sign(world, order, "Withheld", "BP 85/50; GP advised to hold");

        Assert.True(result.IsSuccess);
        Assert.Equal("BP 85/50; GP advised to hold", Assert.Single(world.Administrations.Saved).Reason);
    }

    [Fact]
    public async Task A_controlled_drug_dose_needs_a_witness()
    {
        var (world, order) = Round(controlledDrug: true);

        var result = await Sign(world, order);

        Assert.Equal("Medications.WitnessRequired", result.Error.Code);
        Assert.Empty(world.Administrations.Saved);
    }

    [Fact]
    public async Task The_witness_cannot_be_the_person_giving_the_dose()
    {
        var (world, order) = Round(controlledDrug: true);

        var result = await Sign(world, order, witness: MedicationWorld.Carer);

        Assert.Equal("Medications.WitnessMustBeSomeoneElse", result.Error.Code);
    }

    [Fact]
    public async Task The_witness_must_be_staff_in_the_home()
    {
        var (world, order) = Round(controlledDrug: true);

        var result = await Sign(world, order, witness: Guid.NewGuid());

        Assert.Equal("Medications.WitnessNotFound", result.Error.Code);
    }

    [Fact]
    public async Task A_witnessed_controlled_drug_dose_records_the_witness()
    {
        var (world, order) = Round(controlledDrug: true);

        var result = await Sign(world, order, witness: MedicationWorld.Witness);

        Assert.True(result.IsSuccess);
        Assert.Equal(MedicationWorld.Witness, result.Value.WitnessedByStaffId);
        Assert.Equal(MedicationWorld.Witness, Assert.Single(world.Administrations.Saved).WitnessedByStaffId);
    }

    [Fact]
    public async Task A_controlled_drug_given_as_required_needs_a_witness_too()
    {
        var world = new MedicationWorld(Eight);
        var order = world.AddPrnOrder(Day.AddDays(-1), Eight.AddDays(-1), 240, 4, controlledDrug: true);

        var result = await world.Medications.RecordPrnAdministration(MedicationWorld.Resident, order.Id.Value,
            new RecordPrnAdministrationRequest("Taken", "5mg", "Breakthrough pain"), MedicationWorld.Carer, CancellationToken.None);

        Assert.Equal("Medications.WitnessRequired", result.Error.Code);
    }

    [Fact]
    public async Task A_vomited_prn_dose_counts_towards_the_24_hour_limit()
    {
        var world = new MedicationWorld(Eight);
        var order = world.AddPrnOrder(Day.AddDays(-1), Eight.AddDays(-1), null, 1);
        var vomited = await world.Medications.RecordPrnAdministration(MedicationWorld.Resident, order.Id.Value,
            new RecordPrnAdministrationRequest("Vomited", "1 g", "Headache"), MedicationWorld.Carer, CancellationToken.None);
        Assert.True(vomited.IsSuccess);
        world.Clock.Now = Eight.AddHours(5);

        var again = await world.Medications.RecordPrnAdministration(MedicationWorld.Resident, order.Id.Value,
            new RecordPrnAdministrationRequest("Taken", "1 g", "Headache"), MedicationWorld.Carer, CancellationToken.None);

        Assert.Equal("Medications.PrnDailyLimitReached", again.Error.Code);
    }
}
