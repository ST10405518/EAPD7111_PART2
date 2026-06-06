using EAPD7111_PART2.Helpers;
using EAPD7111_PART2.Services.Api;
using GLMS.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace EAPD7111_PART2.Controllers;

public class ClientController : Controller
{
    private readonly IGlmsApiClient _apiClient;
    private readonly ILogger<ClientController> _logger;

    public ClientController(IGlmsApiClient apiClient, ILogger<ClientController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var clients = await _apiClient.GetClientsAsync();
            return View(clients);
        }
        catch (ApiClientException ex)
        {
            var unauthorized = ApiErrorHelper.HandleUnauthorized(this, ex);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            _logger.LogError(ex, "Failed to load clients. Status={StatusCode}", ex.StatusCode);
            TempData["Error"] = ApiErrorHelper.GetUserMessage(ex, "Could not load clients from the API.");
            return View(Array.Empty<Client>());
        }
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            var client = await _apiClient.GetClientAsync(id.Value);
            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }
        catch (ApiClientException ex)
        {
            var unauthorized = ApiErrorHelper.HandleUnauthorized(this, ex);
            if (unauthorized != null)
            {
                return unauthorized;
            }

            _logger.LogError(ex, "Failed to load client {ClientId}. Status={StatusCode}", id, ex.StatusCode);
            TempData["Error"] = ApiErrorHelper.GetUserMessage(ex, "Could not load client details from the API.");
            return RedirectToAction(nameof(Index));
        }
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ClientId,Name,Email,PhoneNumber,Address,Region,ContactPerson")] Client client)
    {
        if (ModelState.IsValid)
        {
            try
            {
                client.CreatedDate = DateTime.UtcNow;
                await _apiClient.CreateClientAsync(client);
                TempData["Success"] = "Client created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (ApiClientException ex)
            {
                _logger.LogError(ex, "Failed to create client");
                ModelState.AddModelError(string.Empty, "Could not save the client. Please check your input and try again.");
            }
        }

        return View(client);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            var client = await _apiClient.GetClientAsync(id.Value);
            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }
        catch (ApiClientException ex)
        {
            _logger.LogError(ex, "Failed to load client {ClientId} for edit", id);
            TempData["Error"] = "Could not load the client for editing.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ClientId,Name,Email,PhoneNumber,Address,Region,ContactPerson,CreatedDate")] Client client)
    {
        if (id != client.ClientId)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                await _apiClient.UpdateClientAsync(id, client);
                TempData["Success"] = "Client updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (ApiClientException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
            catch (ApiClientException ex)
            {
                _logger.LogError(ex, "Failed to update client {ClientId}", id);
                ModelState.AddModelError(string.Empty, "Could not update the client. Please try again.");
            }
        }

        return View(client);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            var client = await _apiClient.GetClientAsync(id.Value);
            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }
        catch (ApiClientException ex)
        {
            _logger.LogError(ex, "Failed to load client {ClientId} for delete", id);
            TempData["Error"] = "Could not load the client.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _apiClient.DeleteClientAsync(id);
            TempData["Success"] = "Client deleted successfully.";
        }
        catch (ApiClientException ex)
        {
            _logger.LogError(ex, "Failed to delete client {ClientId}", id);
            TempData["Error"] = "Could not delete the client.";
        }

        return RedirectToAction(nameof(Index));
    }
}
