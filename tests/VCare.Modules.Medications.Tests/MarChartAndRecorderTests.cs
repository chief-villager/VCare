using Medications.Application.Services;
using Medications.Domain.Entities;
using VCare.SharedKernel.Results;
using Xunit;

namespace VCare.Modules.Medications.Tests;

public class MarChartTests
{
    private static readonly DateOnly First = new(2026, 11, 1);
    private static DateTime At(int day, int hour, int minute = 0) => new(2026, 11, day, hour, minute, 0);

    private static async Task<MarChartResponse> November(MedicationWorld world)
    {
        var chart = await world.Chart.BuildChart(MedicationWorld.Resident, 2026, 11, CancellationToken.None);
        Assert.True(chart.IsSuccess, chart.IsFailure ? chart.Error.ToString() : null);
        return chart.Value;
    }

    private static async Task Sign(MedicationWorld world, MedicationOrder order, DateTime slot, string outcome, DateTime signedAt)
    {
        world.Clock.Now = signedAt;
        var result = await world.Medications.RecordAministration(MedicationWorld.Resident, order.Id.Value, world.ScheduleIdOf(order),
            new RecordAdministrationRequest(slot, outcome, null), MedicationWorld.Carer, CancellationToken.None);
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.ToString() : null);
    }

    [Fact]
    public async Task A_signed_square_shows_whether_the_dose_was_taken_or_refused()
    {
        var world = new MedicationWorld(At(1, 7));
        var order = world.AddDailyOrder(First, At(1, 7), new TimeOnly(8, 0), new TimeOnly(20, 0));
        await Sign(world, order, At(1, 8), "Taken", At(1, 8, 5));
        await Sign(world, order, At(1, 20), "Refused", At(1, 20, 5));
        world.Clock.Now = At(2, 7);

        var cells = (await November(world)).Rows.Single().Cells;

        Assert.Equal("T", cells.Single(c => c.DueAt == At(1, 8)).OutcomeLetter);
        Assert.Equal("Taken", cells.Single(c => c.DueAt == At(1, 8)).OutcomeName);
        Assert.Equal("R", cells.Single(c => c.DueAt == At(1, 20)).OutcomeLetter);
    }

    [Fact]
    public async Task A_dose_shows_as_due_in_the_hour_before_its_time()
    {
        var world = new MedicationWorld(At(1, 7, 15));
        world.AddDailyOrder(First, At(1, 6), new TimeOnly(8, 0));

        var cells = (await November(world)).Rows.Single().Cells;

        Assert.Equal(MarCellState.Due, cells.Single(c => c.DueAt == At(1, 8)).State);
    }

    [Fact]
    public async Task Prn_doses_appear_on_the_chart_as_timestamped_entries()
    {
        var world = new MedicationWorld(At(3, 14));
        var order = world.AddPrnOrder(First, At(1, 6), 240, 4);
        var given = await world.Medications.RecordPrnAdministration(MedicationWorld.Resident, order.Id.Value,
            new RecordPrnAdministrationRequest("Taken", "1 tablet", "Headache"), MedicationWorld.Carer, CancellationToken.None);
        Assert.True(given.IsSuccess, given.IsFailure ? given.Error.ToString() : null);

        var row = (await November(world)).Rows.Single();

        Assert.True(row.IsPrn);
        var cell = Assert.Single(row.Cells);
        Assert.Null(cell.DueAt);
        Assert.Equal(At(3, 14), cell.AdministeredAt);
        Assert.Equal("1 tablet", cell.Dose);
        Assert.Equal("T", cell.OutcomeLetter);
        Assert.Equal("Headache", cell.Reason);
    }

    [Fact]
    public async Task A_discontinued_order_keeps_the_doses_that_were_due_before_it_stopped()
    {
        var world = new MedicationWorld(At(1, 7));
        var order = world.AddDailyOrder(First, At(1, 7), new TimeOnly(8, 0));
        await Sign(world, order, At(1, 8), "Taken", At(1, 8, 5));
        world.Clock.Now = At(2, 12);
        var stopped = await world.Medications.UpdateMedicationOrderStatusAsync(MedicationWorld.Resident, order.Id.Value,
            new UpdateMedicationOrderStatusRequest("Discontinued", "Course complete"), MedicationWorld.Carer, CancellationToken.None);
        Assert.True(stopped.IsSuccess);
        world.Clock.Now = At(5, 12);

        var cells = (await November(world)).Rows.Single().Cells;

        // The 1st was signed, the 2nd was missed while the order was live, and
        // nothing is expected from the 3rd on.
        Assert.Equal([At(1, 8), At(2, 8)], cells.Select(c => c.DueAt!.Value).ToList());
        Assert.Equal(MarCellState.Signed, cells[0].State);
        Assert.Equal(MarCellState.Missed, cells[1].State);
    }

    [Fact]
    public async Task Doses_whose_window_closed_before_the_order_was_entered_are_not_shown_as_missed()
    {
        // Admitted with the order backdated to the 1st, entered on the 3rd at 10:00.
        var world = new MedicationWorld(At(3, 12));
        world.AddDailyOrder(First, At(3, 10), new TimeOnly(8, 0), new TimeOnly(20, 0));

        var cells = (await November(world)).Rows.Single().Cells;

        Assert.Equal(MarCellState.NotRecorded, cells.Single(c => c.DueAt == At(2, 20)).State);
        Assert.Equal(MarCellState.NotRecorded, cells.Single(c => c.DueAt == At(3, 8)).State);
        Assert.Equal(MarCellState.NotDue, cells.Single(c => c.DueAt == At(3, 20)).State);
    }

    [Fact]
    public async Task An_impossible_month_is_a_bad_request_not_a_crash()
    {
        var world = new MedicationWorld(At(1, 7));

        var chart = await world.Chart.BuildChart(MedicationWorld.Resident, 2026, 13, CancellationToken.None);

        Assert.Equal(ErrorKind.Validation, chart.Error.Kind);
    }
}

public class MissedDoseRecorderTests
{
    private static DateTime At(int day, int hour, int minute = 0) => new(2026, 11, day, hour, minute, 0);

    [Fact]
    public async Task Doses_whose_window_closed_before_the_order_was_entered_are_not_recorded_missed()
    {
        // Backdated three days at admission and entered at 09:30, after the 08:00
        // window had closed: none of those doses were VCare's to miss.
        var world = new MedicationWorld(At(19, 9, 35));
        world.AddDailyOrder(new DateOnly(2026, 11, 16), At(19, 9, 30), new TimeOnly(8, 0), new TimeOnly(20, 0));

        var recorded = await world.Recorder.RecordMissedDosesAsync(CancellationToken.None);

        Assert.Equal(0, recorded);
        Assert.Empty(world.Administrations.Saved);
    }

    [Fact]
    public async Task A_dose_due_after_the_order_was_entered_is_recorded_missed_once_its_window_closes()
    {
        var world = new MedicationWorld(At(19, 9, 5));
        world.AddDailyOrder(new DateOnly(2026, 11, 19), At(19, 7, 0), new TimeOnly(8, 0));

        var recorded = await world.Recorder.RecordMissedDosesAsync(CancellationToken.None);

        Assert.Equal(1, recorded);
        var missed = Assert.Single(world.Administrations.Saved);
        Assert.True(missed.IsMissed);
        Assert.Equal(At(19, 8), missed.ScheduledFor);
    }
}
