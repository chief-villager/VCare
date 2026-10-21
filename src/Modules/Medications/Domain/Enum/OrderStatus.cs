using VCare.SharedKernel.Results;

namespace Medications.Domain.Enum
{
    public enum OrderStatus
    {
        Active,
        Discontinued,
        Suspended
    }

public static class OrderStatusEnumCheker
{
    private static readonly Dictionary<string, OrderStatus> Order_Status = 
    System.Enum.GetValues<OrderStatus>()
                .ToDictionary(r => r.ToString(), StringComparer.OrdinalIgnoreCase);
    public static Result<OrderStatus> OrderStatusCheker(string status)
    {
        if (!string.IsNullOrWhiteSpace(status) && Order_Status.TryGetValue(status.Trim(), out var parsed))
                return Result.Success(parsed);

            return Result.Failure<OrderStatus>(
                new Error("400",
                    $"Invalid order status '{status}'. Expected one of: {string.Join(", ", Order_Status.Keys)}.",
                    ErrorKind.Validation));
    }
}
   
}