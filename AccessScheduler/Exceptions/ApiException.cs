using AccessScheduler.Shared.DTOs;

namespace AccessScheduler.Blazor.Services;

public class ApiException : Exception
{
    public ApiException(string message) : base(message) { }
    public ApiException(string message, Exception innerException) : base(message, innerException) { }
}