using EAPD7111_PART2.Helpers;
using EAPD7111_PART2.Services.Api;
using GLMS.Shared.Dtos;
using GLMS.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace EAPD7111_PART2.Controllers;

public class ServiceRequestController : Controller
{
    public const decimal FallbackUsdToZarRate = 18.50m;

    private readonly IGlmsApiClient _apiClient;
    private readonly ILogger<ServiceRequestController> _logger;

    public ServiceRequestController(IGlmsApiClient apiClient, ILogger<ServiceRequestController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var serviceRequests = await _apiClient.GetServiceRequestsAsync();
            return View(serviceRequests.OrderByDescending(s => s.CreatedDate).ToList());
        }
        catch (ApiClientException ex)
        {
            _logger.LogError(ex, "Failed to load service requests");
            TempData["Error"] = "Could not load service requests from the API. Please sign in and try again.";
            return View(Array.Empty<ServiceRequest>());
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
            var serviceRequest = await _apiClient.GetServiceRequestAsync(id.Value);
            if (serviceRequest == null)
            {
                return NotFound();
            }

            return View(serviceRequest);
        }
        catch (ApiClientException ex)
        {
            _logger.LogError(ex, "Failed to load service request {ServiceRequestId}", id);
            TempData["Error"] = "Could not load service request details from the API.";
            return RedirectToAction(nameof(Index));
        }
    }

    public async Task<IActionResult> Create()
    {
        try
        {
            await SetContractDropdownAsync();
            return View();
        }
        catch (ApiClientException ex)
        {
            _logger.LogError(ex, "Failed to prepare service request create form");
            TempData["Error"] = "Could not load contracts from the API.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ServiceRequestId,ContractId,RequestNumber,Description,CostUSD,Status,Notes")] ServiceRequest serviceRequest)
    {
        TryBindContractIdFromForm(serviceRequest);

        if (ModelState.IsValid)
        {
            try
            {
                var dto = new CreateServiceRequestDto
                {
                    ContractId = serviceRequest.ContractId,
                    RequestNumber = serviceRequest.RequestNumber,
                    Description = serviceRequest.Description,
                    CostUSD = serviceRequest.CostUSD,
                    Status = serviceRequest.Status,
                    Notes = serviceRequest.Notes
                };

                await _apiClient.CreateServiceRequestAsync(dto);
                TempData["Success"] = "Service request created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (ApiClientException ex)
            {
                _logger.LogError(ex, "Failed to create service request");
                ModelState.AddModelError(string.Empty, "Could not save the service request. Please check the contract status and try again.");
            }
        }

        await SetContractDropdownAsync(serviceRequest.ContractId);
        return View(serviceRequest);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            var serviceRequest = await _apiClient.GetServiceRequestAsync(id.Value);
            if (serviceRequest == null)
            {
                return NotFound();
            }

            await SetContractDropdownAsync(serviceRequest.ContractId, activeOnly: false);
            return View(serviceRequest);
        }
        catch (ApiClientException ex)
        {
            _logger.LogError(ex, "Failed to load service request {ServiceRequestId} for edit", id);
            TempData["Error"] = "Could not load the service request for editing.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("ServiceRequestId,ContractId,RequestNumber,Description,CostUSD,CostZAR,Status,Notes,CreatedDate")] ServiceRequest serviceRequest)
    {
        if (id != serviceRequest.ServiceRequestId)
        {
            return NotFound();
        }

        TryBindContractIdFromForm(serviceRequest);

        if (ModelState.IsValid)
        {
            try
            {
                await _apiClient.UpdateServiceRequestAsync(id, serviceRequest);
                TempData["Success"] = "Service request updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (ApiClientException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
            catch (ApiClientException ex)
            {
                _logger.LogError(ex, "Failed to update service request {ServiceRequestId}", id);
                ModelState.AddModelError(string.Empty, "Could not update the service request. Please try again.");
            }
        }

        await SetContractDropdownAsync(serviceRequest.ContractId, activeOnly: false);
        return View(serviceRequest);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            var serviceRequest = await _apiClient.GetServiceRequestAsync(id.Value);
            if (serviceRequest == null)
            {
                return NotFound();
            }

            return View(serviceRequest);
        }
        catch (ApiClientException ex)
        {
            _logger.LogError(ex, "Failed to load service request {ServiceRequestId} for delete", id);
            TempData["Error"] = "Could not load the service request.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _apiClient.DeleteServiceRequestAsync(id);
            TempData["Success"] = "Service request deleted successfully.";
        }
        catch (ApiClientException ex)
        {
            _logger.LogError(ex, "Failed to delete service request {ServiceRequestId}", id);
            TempData["Error"] = "Could not delete the service request.";
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> GetExchangeRate()
    {
        try
        {
            var response = await _apiClient.GetExchangeRateAsync();
            return Json(new { rate = response.Rate, timestamp = response.Timestamp });
        }
        catch (ApiClientException ex)
        {
            _logger.LogWarning(ex, "Failed to fetch exchange rate from API");
            return Json(new { rate = FallbackUsdToZarRate, timestamp = DateTime.UtcNow });
        }
    }

    private async Task SetContractDropdownAsync(int? selectedContractId = null, bool activeOnly = true)
    {
        var contracts = await _apiClient.GetContractsAsync(
            status: activeOnly ? ContractStatus.Active : null);

        var orderedContracts = contracts.OrderBy(c => c.ContractNumber).ToList();
        ViewBag.ContractList = DropdownHelper.BuildContractList(orderedContracts, selectedContractId);
    }

    private void TryBindContractIdFromForm(ServiceRequest serviceRequest)
    {
        ModelState.Remove("Contract");
        foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Contract.", StringComparison.Ordinal)).ToList())
        {
            ModelState.Remove(key);
        }

        if (serviceRequest.ContractId > 0)
        {
            return;
        }

        if (int.TryParse(Request.Form["ContractId"], out var contractId) && contractId > 0)
        {
            serviceRequest.ContractId = contractId;
            ModelState.Remove(nameof(serviceRequest.ContractId));
        }
    }
}
