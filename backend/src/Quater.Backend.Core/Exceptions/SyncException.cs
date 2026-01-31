namespace Quater.Backend.Core.Exceptions;

/// <summary>
/// Exception thrown when a synchronization operation fails.
/// </summary>
public class SyncException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncException"/> class.
    /// </summary>
    public SyncException()
        : base("A synchronization error occurred.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public SyncException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public SyncException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
