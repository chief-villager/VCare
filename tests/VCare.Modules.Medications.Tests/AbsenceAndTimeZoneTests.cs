using Medications.Application.Services;
using Medications.Domain.Entities;
using VCare.SharedKernel.Abstractions;
using Xunit;

namespace VCare.Modules.Medications.Tests;

// A resident admitted to hospital on the 19th at 10:00. Their 08:00 and 20:00
// doses go on being scheduled, but the home cannot give them.
public class AbsenceTests
{
    private static DateTime At(int day, int hour, int minute = 0) => new(2026, 11, day, hour, minute, 0);

    private static (MedicationWorld World, MedicationOrder Order) TwiceDaily(DateTime now)
    {
        var world = new MedicationWorld(now);
        var order = world.AddDailyOrder(new DateOnly(2026, 11, 19), At(18, 18), new TimeOnly(8, 0), new TimeOnly(20, 0));
        return (world, order);
    }

    private static string OutcomeOf(MedicationAdministration a) =>
        OutcomeCode.Seeded.Single(o => o.Id == a.OutcomeCodeId).Name;

    [Fact]
    public async Task Doses_due_while_the_resident_is_in_hospital_are_recorded_as_in_hospital_not_missed()
    {
        var (world, _) = TwiceDaily(At(20, 9, 30));
        world.Absences.Add(new ResidentAbsence(MedicationWorld.Resident, AbsenceKind.Hospital, At(19, 10), null));

        await world.Recorder.RecordMissedDosesAsync(CancellationToken.None);

        var bySlot = world.Administrations.Saved.ToDictionary(a => a.ScheduledFor!.Value, OutcomeOf);
        Assert.Equal("Missed", bySlot[At(19, 8)]);
        Assert.Equal("In hospital", bySlot[At(19, 20)]);
        Assert.Equal("In hospital", bySlot[At(20, 8)]);
    }

    [Fact]
    public async Task Doses_due_on_leave_are_recorded_as_on_leave_and_misses_resume_on_return()
    {
        var (world, _) = TwiceDaily(At(20, 9, 30));
        world.Absences.Add(new ResidentAbsence(MedicationWorld.Resident, AbsenceKind.Leave, At(19, 10), At(19, 22)));

        await world.Recorder.RecordMissedDosesAsync(CancellationToken.None);

        var bySlot = world.Administrations.Saved.ToDictionary(a => a.ScheduledFor!.Value, OutcomeOf);
        Assert.Equal("On leave", bySlot[At(19, 20)]);
        Assert.Equal("Missed", bySlot[At(20, 8)]);
    }

    [Fact]
    public async Task The_outstanding_list_leaves_out_doses_due_while_the_resident_is_away()
    {
        var (world, _) = TwiceDaily(At(19, 19, 30));
        world.Absences.Add(new ResidentAbsence(MedicationWorld.Resident, AbsenceKind.Hospital, At(19, 10), null));

        var outstanding = (await world.Medications.GetOutstandingForDay(MedicationWorld.Resident,
            new DateOnly(2026, 11, 19), CancellationToken.None)).Value;

        Assert.Equal([At(19, 8)], outstanding.Select(s => s.DueAt).ToList());
    }

    [Fact]
    public async Task The_chart_shows_a_dose_due_while_away_as_away_rather_than_missed()
    {
        var (world, _) = TwiceDaily(At(20, 12));
        world.Absences.Add(new ResidentAbsence(MedicationWorld.Resident, AbsenceKind.Hospital, At(19, 10), null));

        var cells = (await world.Chart.BuildChart(MedicationWorld.Resident, 2026, 11, CancellationToken.None)).Value.Rows.Single().Cells;

        Assert.Equal(MarCellState.Missed, cells.Single(c => c.DueAt == At(19, 8)).State);
        Assert.Equal(MarCellState.Away, cells.Single(c => c.DueAt == At(19, 20)).State);
    }
}

// Dose times are times in the care home, whatever the server's clock says.
public class CareHomeTimeZoneTests
{
    private static readonly DateOnly July1 = new(2026, 7, 1);
    private static DateTime Utc(int hour, int minute = 0) => July1.ToDateTime(new TimeOnly(hour, minute));

    [Fact]
    public async Task Sign_off_uses_the_time_in_the_care_home()
    {
        // 08:30 UTC is 09:30 in London in July: the 08:00 dose's on-time window has closed.
        var world = new MedicationWorld(Utc(8, 30));
        world.HomeClock.Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
        var order = world.AddDailyOrder(July1, July1.AddDays(-1).ToDateTime(new TimeOnly(18, 0)), new TimeOnly(8, 0));
        var slot = July1.ToDateTime(new TimeOnly(8, 0));

        var unexplained = await world.Medications.RecordAministration(MedicationWorld.Resident, order.Id.Value, world.ScheduleIdOf(order),
            new RecordAdministrationRequest(slot, "Taken", null), MedicationWorld.Carer, CancellationToken.None);
        var explained = await world.Medications.RecordAministration(MedicationWorld.Resident, order.Id.Value, world.ScheduleIdOf(order),
            new RecordAdministrationRequest(slot, "Taken", null, null, "Resident was at breakfast"), MedicationWorld.Carer, CancellationToken.None);

        Assert.Equal("Medications.LateReasonRequired", unexplained.Error.Code);
        Assert.True(explained.IsSuccess);
        Assert.Equal(July1.ToDateTime(new TimeOnly(9, 30)), explained.Value.AdministeredAt);
    }

    [Fact]
    public async Task The_missed_dose_recorder_uses_the_time_in_the_care_home()
    {
        // 12:30 UTC is 08:30 in New York in July: the 08:00 dose is still on time.
        var world = new MedicationWorld(Utc(12, 30));
        world.HomeClock.Zone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        world.AddDailyOrder(July1, July1.AddDays(-1).ToDateTime(new TimeOnly(18, 0)), new TimeOnly(8, 0));

        var early = await world.Recorder.RecordMissedDosesAsync(CancellationToken.None);
        world.Clock.Now = Utc(13, 30);
        var late = await world.Recorder.RecordMissedDosesAsync(CancellationToken.None);

        Assert.Equal(0, early);
        Assert.Equal(1, late);
    }
}
