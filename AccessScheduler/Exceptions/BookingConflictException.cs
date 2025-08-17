using AccessScheduler.Shared.DTOs;

public class BookingConflictException : Exception
{
    public ConflictResponse ConflictResponse { get; }

    public BookingConflictException(string message, ConflictResponse? conflictResponse) : base(message)
    {
        ConflictResponse = conflictResponse ?? new ConflictResponse();
    }
}