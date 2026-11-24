using VCare.SharedKernel.Auditing;
using Medications.Domain.Enum;
using VCare.SharedKernel.Abstractions;

namespace Medications.Domain.Entities
{
    /// <summary>
    /// One change to an order's status: who held, restarted or stopped it, when,
    /// and why. The history is what lets a suspended order be restarted without
    /// losing which doses were expected while it was held: those doses were never
    /// due, so they are neither missed nor signable.
    /// </summary>
    internal sealed class OrderStatusChange : IAudited
    {
        public const int ReasonMaxLength = 500;

        public Guid Id { get; private set; }
        public MedicationOrderId MedicationOrderId { get; private set; }
        public OrderStatus From { get; private set; }
        public OrderStatus To { get; private set; }

        // Wall-clock time in the care home, like dose times.
        public DateTime ChangedAt { get; private set; }
        public Guid ChangedByStaffId { get; private set; }
        public string Reason { get; private set; } = null!;

        private OrderStatusChange() { }

        internal static OrderStatusChange Create(MedicationOrderId orderId, OrderStatus from, OrderStatus to,
            DateTime changedAt, Guid changedByStaffId, string reason) =>
            new()
            {
                Id = Guid.NewGuid(),
                MedicationOrderId = orderId,
                From = from,
                To = to,
                ChangedAt = changedAt,
                ChangedByStaffId = changedByStaffId,
                Reason = reason
            };
    }
}
