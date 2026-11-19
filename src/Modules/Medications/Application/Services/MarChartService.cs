using Medications.Application.Abstracts;
using Medications.Application.Services.Interfaces;
using Medications.Domain.Entities;
using Medications.Domain.Enum;
using Medications.Domain.Errors;
using VCare.SharedKernel.Results;

namespace Medications.Application.Services
{
    internal class MarChartService (IMedicationOrderRepository _orders,
    IMedicalAdministrationRepository _admins, ScheduleExpander _expander,
    IOutcomeRepository _outcomes, TimeProvider _clock) : IMarChartService
    {
        public async Task<Result<MarChartResponse>> BuildChart(Guid patientId, int year, int month, CancellationToken token)
        {
            // new DateOnly(2026, 13, 1) throws; a bad query string is the caller's
            // mistake and gets a 400, not a 500.
            if (year is < 1 or > 9999 || month is < 1 or > 12)
                return Result.Failure<MarChartResponse>(MedicationErrors.InvalidMonth);

            // Month boundaries. "1st of next month minus a day" = last day, any length.
            var from = new DateOnly(year, month, 1);
            var to   = from.AddMonths(1).AddDays(-1);

            // Every order live this month (one row each), every recorded dose this
            // month, and the outcome codes so each square can say what happened.
            var orders = (await _orders.ActiveBetween(patientId, from, to)).ToList();
            var admins = await _admins.ForResidentBetweenAsync(patientId, from, to, token);
            var outcomes = (await _outcomes.GetAllAsync(token)).ToDictionary(o => o.Id);

            // Scheduled doses, re-filed by (order, slot) so each square finds its
            // signature instantly: (paracetamol, 1 Aug 08:00) → SA's signature.
            var scheduledLookup = admins.Where(a => a.ScheduledFor.HasValue)
                .ToLookup(a => (a.MedicationOrderId.Value, a.ScheduledFor!.Value));
            var prnLookup = admins.Where(a => a.ScheduledFor is null)
                .ToLookup(a => a.MedicationOrderId.Value);

            var now = _clock.GetLocalNow().DateTime;
            OutcomeCode? OutcomeOf(MedicationAdministration? a) =>
                a is not null && outcomes.TryGetValue(a.OutcomeCodeId, out var o) ? o : null;

            var rows = orders.Select(order =>
            {
                if (order.IsPrn)
                {
                    // As-required doses have no plan to expand: the row is the doses
                    // that were given (or offered), in time order.
                    var entries = prnLookup[order.Id.Value]
                        .OrderBy(a => a.AdministeredAt)
                        .Select(a => new MarCell
                        {
                            DueAt = null,
                            Dose = a.DoseGiven,
                            Administration = a,
                            Outcome = OutcomeOf(a),
                            Now = now,
                            OrderEnteredAt = order.CreatedAt
                        })
                        .ToList();
                    return new MarRow { Order = order, Cells = entries };
                }

                var cells = _expander.ExpandPlanned(order, from, to)
                    .Select(slot => (Slot: slot, Administration: scheduledLookup[(order.Id.Value, slot.DueAt)].FirstOrDefault()))
                    .Where(x => IsPartOfRecord(order, x.Slot.DueAt, x.Administration))
                    .Select(x => new MarCell
                    {
                        DueAt = x.Slot.DueAt,
                        Dose = x.Slot.Dose,
                        Administration = x.Administration,
                        Outcome = OutcomeOf(x.Administration),
                        Now = now,
                        OrderEnteredAt = order.CreatedAt
                    })
                    .ToList();
                return new MarRow { Order = order, Cells = cells };
            });

            var chart = new MarChart { Rows = rows.ToList() };

            // The chart above is the domain shape; below is the wire shape. Keeping
            // them apart is what lets MarCell own IsSigned/IsMissed while the
            // response hands the client one state it cannot misread.
            return Result.Success(ToResponse(chart, patientId, year, month, from, to));
        }

        // A planned dose belongs on the chart while the order was live. Once it is
        // suspended or discontinued, doses due before that moment stay (they are
        // the resident's record); later ones were never expected. An order stopped
        // before the change time was kept still shows every dose someone recorded.
        private static bool IsPartOfRecord(MedicationOrder order, DateTime dueAt, MedicationAdministration? administration)
        {
            if (order.Status == OrderStatus.Active || administration is not null)
                return true;
            return order.StatusChangedAt is { } stoppedAt && dueAt < stoppedAt;
        }

        private static MarChartResponse ToResponse(
            MarChart chart, Guid patientId, int year, int month, DateOnly from, DateOnly to) =>
            new(
                patientId,
                year,
                month,
                from,
                to,
                [.. chart.Rows.Select(ToResponse)]);

        private static MarRowResponse ToResponse(MarRow row) =>
            new(
                row.Order.Id.Value,
                row.Order.Medication,
                row.Order.Route,
                row.Order.Instructions,
                row.Order.IsPrn,
                row.Order.PrnIndication,
                row.Order.IsControlledDrug,
                row.Order.Status,
                [.. row.Cells.Select(ToResponse)]);

        private static MarCellResponse ToResponse(MarCell cell) =>
            new(
                cell.DueAt,
                cell.Dose,
                cell.IsSigned ? MarCellState.Signed
                    : cell.IsMissed ? MarCellState.Missed
                    : cell.IsBeforeOrderEntered ? MarCellState.NotRecorded
                    : cell.IsDue ? MarCellState.Due
                    : MarCellState.NotDue,
                cell.Administration?.Id.Value,
                // Taken and Refused are both "signed"; the letter is what tells a
                // reader whether the resident actually had the medicine.
                cell.Outcome?.DisplayLetter,
                cell.Outcome?.Name,
                cell.Administration?.AdministeredAt,
                cell.Administration?.AdministeredByStaffId,
                cell.Administration?.Notes,
                cell.Administration?.IsLate ?? false,
                cell.Administration?.Reason);
    }
}
