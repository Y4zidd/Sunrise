namespace Sunrise.API.Serializable.Response;

public class ErrorResponse
{
    public string Message { get; set; }

    public ErrorResponse(string message)
    {
        Message = message;
    }
}
