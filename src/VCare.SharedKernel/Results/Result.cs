namespace VCare.SharedKernel.Results;

public class Result
{
    protected Result(bool isSuccess, string error)
    {
        if (isSuccess && string.IsNullOrEmpty(error))
            throw new InvalidOperationException("A successful result cannot carry an error.");
        if (!isSuccess && string.IsNullOrEmpty(error))
            throw new InvalidOperationException("A failed result must carry an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }

    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, string.Empty);
    public static Result<TValue> Failure<TValue>(string error) => new(default!, false, error);
}

public sealed class Result<TValue> : Result
{
    private readonly TValue _value;

    internal Result(TValue value, bool isSuccess, string error) : base(isSuccess, error)
        => _value = value;

    public TValue Value => IsSuccess
        ? _value
        : throw new InvalidOperationException("The value of a failed result cannot be accessed.");
}
