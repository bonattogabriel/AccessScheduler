using AccessScheduler.Shared.DTOs;

namespace AccessScheduler.Shared.Exceptions;

public class ConcurrencyException : Exception
{
    public ConflictResponse ConflictResponse { get; }

    public ConcurrencyException(string message, ConflictResponse conflictResponse) : base(message)
    {
        ConflictResponse = conflictResponse;
    }
}