using System.Net;

namespace EAPD7111_PART2.Services.Api;

public class ApiClientException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public string? ResponseBody { get; }

    public ApiClientException(HttpStatusCode statusCode, string? responseBody)
        : base(GetMessage(statusCode, responseBody))
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    private static string GetMessage(HttpStatusCode statusCode, string? responseBody)
    {
        if (!string.IsNullOrWhiteSpace(responseBody))
        {
            return $"API request failed ({(int)statusCode}): {responseBody}";
        }

        return $"API request failed with status {(int)statusCode} ({statusCode}).";
    }
}
