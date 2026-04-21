namespace HStore.Application.Exceptions;

public class OutOfStockException(string message) : ApiException(message, 409)
{
    
}