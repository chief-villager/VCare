using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Medications.Application.Abstracts;
using Medications.Application.Services.Interfaces;
using Medications.Domain.Entities;

namespace Medications.Application.Services
{
    internal class MarChartService (IMedicationOrderRepository _orders, 
    IMedicalAdministrationRepository _admins, ScheduleExpander _expander ) : IMarChartService
    {
        public async Task<MarChartResponse> BuildChart(Guid patientId, int year, int month)
        {
            // Month boundaries. "1st of next month minus a day" = last day, any length.
            var from = new DateOnly(year, month, 1);
            var to   = from.AddMonths(1).AddDays(-1);

            // TWO database reads:
            //   orders = the rows-to-be (every order live this month)
            var orders = await _orders.ActiveBetween(patientId, from, to);
    
            //   admins = every signed dose this month, RE-FILED by (order, slot) so we
            //   can look one up instantly. Think of ToLookup as building an index:
            //     (paracetamol, 1 Aug 08:00) → SA's signature
            //     (paracetamol, 1 Aug 20:00) → JT's signature
            var admins = _admins.ForResidentBetween(patientId, from, to).ToList();
            var scheduledLookup = admins.Where(a => a.ScheduledFor.HasValue)
                .ToLookup(a => (a.MedicationOrderId.Value, a.ScheduledFor!.Value));

            
                                
            // Build each row: expand the plan, then match each slot to a signature.
            var rows = orders.Select(order =>
            {
                var cells = _expander.ExpandSchedule(order, from, to)     // expected slots
                    .Select(slot => new MarCell
                    {
                        DueAt = slot.DueAt,
                        Dose = slot.Dose,
                        // look in the index under (this order, this slot):
                        //   found    → attach the signature   → IsSigned true
                        //   not found→ FirstOrDefault() = null → blank or missed
                        Administration = scheduledLookup[(order.Id.Value, slot.DueAt)].FirstOrDefault()
                        
                    })
                    .ToList();
    
                return new MarRow { Order = order, Cells = cells };
            });

            var prnRows = BuildPrnRows(orders, scheduledLookup, from, to);

            var chart = new MarChart { Rows = rows.Concat(prnRows).ToList() };

            // The chart above is the domain shape; below is the wire shape. Keeping
            // them apart is what lets MarCell own IsSigned/IsMissed while the
            // response hands the client one state it cannot misread.
            return ToResponse(chart, patientId, year, month, from, to);
    
            // ── TRACE (paracetamol row, "now" = 2 Aug midday) ─────────────────────
            //   Expected slots from ExpandSchedule → matched against the index:
            //
            //     slot 1 Aug 08:00 → index HIT (SA)  → IsSigned=true   → shows "SA"
            //     slot 1 Aug 20:00 → index HIT (JT)  → IsSigned=true   → shows "JT"
            //     slot 2 Aug 08:00 → miss → null → past & unsigned      → IsMissed=true (red gap)
            //     slot 2 Aug 20:00 → miss → null → FUTURE (8pm not here)→ blank, not due yet
            //     slot 3 Aug 08:00 → miss → null → future               → blank
            //       ... etc.
            //
            //   Notice: nobody ever stored "missed" or "blank". Those states are
            //   COMPUTED from whether a signature exists + whether the time has passed.
            //   The chart is the match between plan and events — nothing more.
    

        }


    // PRN row: no expansion, cells are the as-required doses that were given.
        private IEnumerable<MarRow> BuildPrnRows(
            IEnumerable<MedicationOrder> orders,
            ILookup<(Guid, DateTime), MedicationAdministration> admins,
            DateOnly from, DateOnly to)
        {
            // (sketch) for each order where IsPrn == true, gather its administrations
            // straight from the data and present them as timestamped entries, not a grid.
            return [];
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
                    : MarCellState.NotDue,
                cell.Administration?.Id.Value,
                // TODO: an administration carries only OutcomeCodeId. Filling the
                // letter the square displays needs the outcome codes loaded and
                // indexed by id — IOutcomeRepository only looks them up by name.
                null,
                null,
                cell.Administration?.AdministeredAt,
                cell.Administration?.AdministeredByStaffId,
                cell.Administration?.Notes);
    }   
}
