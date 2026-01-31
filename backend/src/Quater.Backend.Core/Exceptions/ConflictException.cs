namespace Quater.Backend.Core.Exceptions;

/// <summary>
/// Exception thrown when a conflict occurs during an operation (e.g., concurrency conflict, duplicate resource).
/// </summary>
public class ConflictException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictException"/> class.
    /// </summary>
    public ConflictException()
        : base("A conflict occurred while processing the request.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ConflictException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
