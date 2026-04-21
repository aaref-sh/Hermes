namespace HStore.Application.Exceptions;

public class UnauthorizedAccessException(string message) : ApiException(message, 401);