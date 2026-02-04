namespace Quater.Backend.Core.Exceptions;

/// <summary>
/// Exception thrown when a request is invalid or malformed.
/// </summary>
public class BadRequestException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BadRequestException"/> class.
    /// </summary>
    public BadRequestException()
        : base("The request is invalid.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BadRequestException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public BadRequestException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BadRequestException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public BadRequestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
