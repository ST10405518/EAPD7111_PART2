using GLMS.Shared.Dtos;
using EAPD7111_PART2.Services.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;

namespace EAPD7111_PART2.Controllers;

public class AccountController : Controller
{
    private readonly IGlmsApiClient _apiClient;
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IGlmsApiClient apiClient,
        IOptions<ApiSettings> apiSettings,
        ILogger<AccountController> logger)
    {
        _apiClient = apiClient;
        _apiSettings = apiSettings.Value;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (Request.Cookies.ContainsKey(ApiTokenHandler.TokenCookieName))
        {
            return RedirectToLocal(returnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        ViewData["ApiBaseUrl"] = _apiSettings.BaseUrl;

        var apiOnline = await _apiClient.IsApiReachableAsync();
        ViewBag.ApiOnline = apiOnline;
        if (!apiOnline)
        {
            ViewBag.ApiWarning =
                $"The GLMS API is not running at {_apiSettings.BaseUrl}. Start GLMS.Api first, then try again.";
        }

        return View(new LoginRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["ApiBaseUrl"] = _apiSettings.BaseUrl;
        ViewBag.ApiOnline = await _apiClient.IsApiReachableAsync();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await _apiClient.IsApiReachableAsync())
        {
            ModelState.AddModelError(string.Empty,
                $"Cannot reach the API at {_apiSettings.BaseUrl}. Start the GLMS.Api project first (port 8080), then sign in again.");
            return View(model);
        }

        try
        {
            var response = await _apiClient.LoginAsync(model);

            if (string.IsNullOrWhiteSpace(response.Token))
            {
                ModelState.AddModelError(string.Empty, "The API returned an empty login token.");
                return View(model);
            }

            var expiresUtc = response.ExpiresAt.Kind switch
            {
                DateTimeKind.Utc => response.ExpiresAt,
                DateTimeKind.Local => response.ExpiresAt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(response.ExpiresAt, DateTimeKind.Utc)
            };

            if (expiresUtc <= DateTime.UtcNow)
            {
                expiresUtc = DateTime.UtcNow.AddHours(1);
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = new DateTimeOffset(expiresUtc)
            };

            Response.Cookies.Append(ApiTokenHandler.TokenCookieName, response.Token, cookieOptions);
            TempData["Success"] = "Signed in successfully.";
            return RedirectToLocal(returnUrl);
        }
        catch (ApiClientException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Invalid login for user {Username}", model.Username);
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
        }
        catch (ApiClientException ex)
        {
            _logger.LogWarning(ex, "API login failed for user {Username}", model.Username);
            ModelState.AddModelError(string.Empty, $"Sign-in failed: {ex.Message}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "API unreachable during login for user {Username}", model.Username);
            ModelState.AddModelError(string.Empty,
                $"Cannot connect to the API at {_apiSettings.BaseUrl}. Make sure GLMS.Api is running.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for user {Username}", model.Username);
            ModelState.AddModelError(string.Empty,
                "Unable to sign in. Check that GLMS.Api is running on http://localhost:8080.");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(ApiTokenHandler.TokenCookieName);
        TempData["Success"] = "Signed out successfully.";
        return RedirectToAction("Index", "Home");
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
