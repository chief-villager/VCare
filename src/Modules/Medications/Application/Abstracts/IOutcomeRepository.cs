using Medications.Domain.Entities;

namespace Medications.Application.Abstracts
{
    internal interface IOutcomeRepository
    {
        Task<OutcomeCode?>GetOutcomeByName(string name, CancellationToken cancellationToken);

        /// <summary>Every outcome code. The MAR chart shows each signed square's letter.</summary>
        Task<IReadOnlyList<OutcomeCode>> GetAllAsync(CancellationToken cancellationToken);
    }
}
