using GLMS.Shared.Dtos;
using EAPD7111_PART2.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace EAPD7111_PART2.Controllers;

public class AccountController : Controller
{
    private readonly IGlmsApiClient _apiClient;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IGlmsApiClient apiClient, ILogger<AccountController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (Request.Cookies.ContainsKey(ApiTokenHandler.TokenCookieName))
        {
            return RedirectToLocal(returnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var response = await _apiClient.LoginAsync(model);
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = response.ExpiresAt.ToUniversalTime()
            };

            Response.Cookies.Append(ApiTokenHandler.TokenCookieName, response.Token, cookieOptions);
            TempData["Success"] = "Signed in successfully.";
            return RedirectToLocal(returnUrl);
        }
        catch (ApiClientException ex)
        {
            _logger.LogWarning(ex, "Login failed for user {Username}", model.Username);
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for user {Username}", model.Username);
            ModelState.AddModelError(string.Empty, "Unable to sign in right now. Please try again later.");
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
