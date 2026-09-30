using Medications.Domain.Enum;

namespace Medications.Application.Services
{
    /// <summary>
    /// A month of MAR chart for one resident: one row per medication order,
    /// one cell per dose the plan expects. The grid is the match between the
    /// plan and the signatures — nothing here is stored, it is all computed.
    /// </summary>
    public sealed record MarChartResponse
    (
        Guid PatientId,
        int Year,
        int Month,
        DateOnly From,
        DateOnly To,
        IReadOnlyList<MarRowResponse> Rows
    );

    /// <summary>One medication order. Scheduled orders render as a grid of slots;
    /// PRN orders render as a list of timestamped entries (see <see cref="MarCellResponse.DueAt"/>).</summary>
    public sealed record MarRowResponse
    (
        Guid OrderId,
        string MedicationName,
        MedicationRouteEnum Route,
        string? Instructions,
        bool IsPrn,
        string? PrnIndication,
        bool IsControlledDrug,
        OrderStatus Status,
        IReadOnlyList<MarCellResponse> Cells
    );

    /// <summary>
    /// One square on the chart. Everything after <see cref="State"/> is null
    /// unless the dose was signed for.
    /// </summary>
    public sealed record MarCellResponse
    (
        // Null on a PRN entry: as-required doses are timestamped, not slotted.
        DateTime? DueAt,
        // Dose sits on the cell, not the row: a phased schedule can change the
        // dose part-way through the month (1 tablet, then 2 from the 14th).
        string? Dose,
        MarCellState State,
        Guid? AdministrationId,
        // What the square displays: "G", "R", "O".
        string? OutcomeLetter,
        string? OutcomeName,
        DateTime? AdministeredAt,
        Guid? AdministeredByStaffId,
        string? Notes
    );

    /// <summary>
    /// The three states a square can be in. One enum rather than the entity's
    /// IsSigned/IsMissed pair, so the wire cannot carry "signed and missed".
    /// </summary>
    public enum MarCellState
    {
        NotDue = 0,   // unsigned, still in the future — blank square
        Missed,       // unsigned, the time has passed — red gap
        Signed        // an administration exists, whatever its outcome
    }
}
