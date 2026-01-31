namespace Quater.Backend.Core.Exceptions;

/// <summary>
/// Exception thrown when a user is authenticated but not authorized to perform an action.
/// </summary>
public class ForbiddenException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenException"/> class.
    /// </summary>
    public ForbiddenException()
        : base("You do not have permission to perform this action.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ForbiddenException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ForbiddenException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ForbiddenException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
