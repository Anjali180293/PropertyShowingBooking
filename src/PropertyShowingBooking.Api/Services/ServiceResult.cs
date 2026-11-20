namespace PropertyShowingBooking.Api.Services;

public sealed class ServiceResult<T>
{
    private ServiceResult(T value)
    {
        IsSuccess = true;
        Value = value;
    }

    private ServiceResult(
        ServiceFailureKind failureKind,
        IDictionary<string, string[]>? errors = null,
        string? errorMessage = null)
    {
        IsSuccess = false;
        FailureKind = failureKind;
        Errors = errors;
        ErrorMessage = errorMessage;
    }

    public bool IsSuccess { get; }
    public T? Value { get; }
    public IDictionary<string, string[]>? Errors { get; }
    public string? ErrorMessage { get; }
    public ServiceFailureKind? FailureKind { get; }

    public static ServiceResult<T> Success(T value) => new(value);

    public static ServiceResult<T> Failure(IDictionary<string, string[]> errors) =>
        new(ServiceFailureKind.Validation, errors);

    public static ServiceResult<T> NotFound(string message) =>
        new(ServiceFailureKind.NotFound, errorMessage: message);

    public static ServiceResult<T> Conflict(string message) =>
        new(ServiceFailureKind.Conflict, errorMessage: message);
}
