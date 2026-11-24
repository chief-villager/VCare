using Medications.Application.Services;
using VCare.SharedKernel.Abstractions;
using Xunit;

namespace VCare.Modules.Medications.Tests;

// A resident discharged, or who died, on the 19th at 07:30. Their orders are
// still active, but no dose is the home's to give from then on: none is
// recorded missed, none can be signed, and nothing is recorded in their name.
public class LeftHomeTests
{
    private static DateTime At(int day, int hour, int minute = 0) => new(2026, 11, day, hour, minute, 0);

    [Theory]
    [InlineData(AbsenceKind.Discharged)]
    [InlineData(AbsenceKind.Deceased)]
    public async Task Doses_due_after_the_resident_left_are_not_recorded_at_all(AbsenceKind left)
    {
        var world = new MedicationWorld(At(20, 9, 30));
        world.AddDailyOrder(new DateOnly(2026, 11, 18), At(18, 6), new TimeOnly(8, 0), new TimeOnly(20, 0));
        world.Absences.Add(new ResidentAbsence(MedicationWorld.Resident, left, At(19, 7, 30), null));

        await world.Recorder.RecordMissedDosesAsync(CancellationToken.None);

        // Only the doses before they left: the 18th at 08:00 and 20:00.
        Assert.Equal([At(18, 8), At(18, 20)], world.Administrations.Saved.Select(a => a.ScheduledFor!.Value).Order());
        Assert.All(world.Administrations.Saved, a => Assert.True(a.IsMissed));
    }

    [Fact]
    public async Task A_death_outweighs_a_hospital_stay_that_overlaps_it()
    {
        // Patients now refuses an absence after a resident has left, but a record
        // made before that rule may still overlap one. Nothing goes on the MAR after the death.
        var world = new MedicationWorld(At(20, 9, 30));
        world.AddDailyOrder(new DateOnly(2026, 11, 19), At(18, 18), new TimeOnly(8, 0));
        world.Absences.Add(new ResidentAbsence(MedicationWorld.Resident, AbsenceKind.Hospital, At(19, 7), null));
        world.Absences.Add(new ResidentAbsence(MedicationWorld.Resident, AbsenceKind.Deceased, At(19, 7, 30), null));

        await world.Recorder.RecordMissedDosesAsync(CancellationToken.None);

        Assert.Empty(world.Administrations.Saved);
    }

    [Fact]
    public async Task A_dose_due_after_the_resident_left_cannot_be_signed()
    {
        var world = new MedicationWorld(At(19, 8, 10));
        var order = world.AddDailyOrder(new DateOnly(2026, 11, 18), At(18, 6), new TimeOnly(8, 0));
        world.Absences.Add(new ResidentAbsence(MedicationWorld.Resident, AbsenceKind.Discharged, At(19, 7, 30), null));

        var result = await world.Medications.RecordAministration(MedicationWorld.Resident, order.Id.Value, world.ScheduleIdOf(order),
            new RecordAdministrationRequest(At(19, 8), "Taken", null), MedicationWorld.Carer, CancellationToken.None);

        Assert.Equal("Medications.ResidentLeftHome", result.Error.Code);
    }

    [Fact]
    public async Task An_as_required_dose_cannot_be_recorded_after_the_resident_died()
    {
        var world = new MedicationWorld(At(19, 9));
        var order = world.AddPrnOrder(new DateOnly(2026, 11, 18), At(18, 6), null, null);
        world.Absences.Add(new ResidentAbsence(MedicationWorld.Resident, AbsenceKind.Deceased, At(19, 7, 30), null));

        var result = await world.Medications.RecordPrnAdministration(MedicationWorld.Resident, order.Id.Value,
            new RecordPrnAdministrationRequest("Taken", "1 tablet", "Pain"), MedicationWorld.Carer, CancellationToken.None);

        Assert.Equal("Medications.ResidentLeftHome", result.Error.Code);
    }

    [Fact]
    public async Task A_hospital_stay_still_records_in_hospital()
    {
        // Leaving for good is different from a stay in hospital, which is still
        // recorded so the MAR shows where every dose went.
        var world = new MedicationWorld(At(19, 9, 30));
        world.AddDailyOrder(new DateOnly(2026, 11, 19), At(18, 18), new TimeOnly(8, 0));
        world.Absences.Add(new ResidentAbsence(MedicationWorld.Resident, AbsenceKind.Hospital, At(19, 7, 30), null));

        await world.Recorder.RecordMissedDosesAsync(CancellationToken.None);

        Assert.Single(world.Administrations.Saved);
        Assert.False(world.Administrations.Saved.Single().IsMissed);
    }
}
