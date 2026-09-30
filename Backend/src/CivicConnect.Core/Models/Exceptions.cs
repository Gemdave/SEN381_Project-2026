namespace CivicConnect.Core.Models;

// The web layer turns each of these into an HTTP status. Core never talks HTTP.

public class UnauthenticatedException(string message) : Exception(message);

public class ForbiddenException(string message) : Exception(message);

public class NotFoundException(string message) : Exception(message);

public class ConflictException(string message) : Exception(message);

public class ValidationFailedException(IDictionary<string, string[]> errors)
    : Exception("One or more fields need attention.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}
