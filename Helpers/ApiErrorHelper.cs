using System.Net;
using EAPD7111_PART2.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace EAPD7111_PART2.Helpers;

public static class ApiErrorHelper
{
    public static IActionResult? HandleUnauthorized(Controller controller, ApiClientException ex)
    {
        if (ex.StatusCode != HttpStatusCode.Unauthorized)
        {
            return null;
        }

        controller.Response.Cookies.Delete(ApiTokenHandler.TokenCookieName);
        controller.TempData["Error"] = "Your session has expired. Please sign in again.";
        return controller.RedirectToAction("Login", "Account");
    }

    public static string GetUserMessage(ApiClientException ex, string defaultMessage)
    {
        return ex.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Your session has expired. Please sign in again.",
            HttpStatusCode.Forbidden => "You do not have permission for this action.",
            HttpStatusCode.NotFound => "The requested record was not found.",
            HttpStatusCode.BadRequest => TryExtractMessage(ex.ResponseBody) ?? defaultMessage,
            _ => $"{defaultMessage} (API error {(int)ex.StatusCode})"
        };
    }

    private static string? TryExtractMessage(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("message", out var message))
            {
                return message.GetString();
            }
        }
        catch
        {
            // ignore parse errors
        }

        return null;
    }
}
