using EAPD7111_PART2.Helpers;
using EAPD7111_PART2.Services.Api;
using GLMS.Shared.Dtos;
using GLMS.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace EAPD7111_PART2.Controllers;

public class ContractController : Controller
{
    private static readonly string[] AllowedPdfExtensions = [".pdf"];

    private readonly IGlmsApiClient _apiClient;
    private readonly ILogger<ContractController> _logger;

    public ContractController(IGlmsApiClient apiClient, ILogger<ContractController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, ContractStatus? status)
    {
        try
        {
            var model = await _apiClient.GetContractsAsync(startDate, endDate, status);

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.Status = status;

            return View(model.OrderByDescending(c => c.CreatedDate).ToList());
        }
        catch (ApiClientException ex)
        {
            _logger.LogError(ex, "Failed to load contracts");
            TempData["Error"] = "Could not load contracts from the API. Please sign in and try again.";
            return View(Array.Empty<Contract>());
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
            var contract = await _apiClient.GetContractAsync(id.Value);
            if (contract == null)
            {
                return NotFound();
            }

            return View(contract);
        }
        catch (ApiClientException ex)
        {
            _logger.LogError(ex, "Failed to load contract {ContractId}", id);
            TempData["Error"] = "Could not load contract details from the API.";
            return RedirectToAction(nameof(Index));
        }
    }

    public async Task<IActionResult> Create()
    {
        try
        {
            var clients = (await _apiClient.GetClientsAsync()).OrderBy(c => c.Name).ToList();
            if (clients.Count == 0)
            {
                TempData["Error"] = "You must create at least one client before adding a contract.";
                return RedirectToAction("Create", "Client");
            }

            SetClientDropdown(clients);
            var contract = new Contract
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddYears(1),
                Status = ContractStatus.Active
            };
            return View(contract);
        }
        catch (ApiClientException ex)
        {
            _logger.LogError(ex, "Failed to prepare contract create form");
            TempData["Error"] = "Could not load clients from the API.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(52_428_800)]
    public async Task<IActionResult> Create(
        [Bind("ContractId,ClientId,ContractNumber,StartDate,EndDate,Status,ServiceLevel,Description")] Contract contract,
        IFormFile? signedAgreement)
    {
        TryBindClientIdFromForm(contract);

        var dateRangeError = ContractValidationService.GetDateRangeErrorMessage(contract.StartDate, contract.EndDate);
        if (dateRangeError != null)
        {
            ModelState.AddModelError(nameof(contract.EndDate), dateRangeError);
        }

        if (signedAgreement != null && signedAgreement.Length > 0 && !IsValidPdf(signedAgreement))
        {
            ModelState.AddModelError("signedAgreement", "Only PDF files are allowed.");
        }

        if (!ModelState.IsValid)
        {
            await SetClientDropdownAsync(contract.ClientId);
            return View(contract);
        }

        try
        {
            var dto = new CreateContractDto
            {
                ClientId = contract.ClientId,
                ContractNumber = contract.ContractNumber,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                Status = contract.Status,
                ServiceLevel = contract.ServiceLevel,
                Description = contract.Description
            };

            await _apiClient.CreateContractAsync(dto, signedAgreement);
            TempData["Success"] = "Contract created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiClientException ex)
        {
            _logger.LogError(ex, "Failed to create contract");
            ModelState.AddModelError(string.Empty, "Could not save the contract. Please check your input and try again.");
        }

        await SetClientDropdownAsync(contract.ClientId);
        return View(contract);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            var contract = await _apiClient.GetContractAsync(id.Value);
            if (contract == null)
            {
                return NotFound();
            }

            await SetClientDropdownAsync(contract.ClientId);
            return View(contract);
        }
        catch (ApiClientException ex)
        {
            _logger.LogError(ex, "Failed to load contract {ContractId} for edit", id);
            TempData["Error"] = "Could not load the contract for editing.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(52_428_800)]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("ContractId,ClientId,ContractNumber,StartDate,EndDate,Status,ServiceLevel,Description,SignedAgreementFilePath,CreatedDate")] Contract contract,
        IFormFile? signedAgreement)
    {
        if (id != contract.ContractId)
        {
            return NotFound();
        }

        TryBindClientIdFromForm(contract);

        var dateRangeError = ContractValidationService.GetDateRangeErrorMessage(contract.StartDate, contract.EndDate);
        if (dateRangeError != null)
        {
            ModelState.AddModelError(nameof(contract.EndDate), dateRangeError);
        }

        if (signedAgreement != null && signedAgreement.Length > 0 && !IsValidPdf(signedAgreement))
        {
            ModelState.AddModelError("signedAgreement", "Only PDF files are allowed.");
        }

        if (!ModelState.IsValid)
        {
            await SetClientDropdownAsync(contract.ClientId);
            return View(contract);
        }

        try
        {
            await _apiClient.UpdateContractAsync(id, contract, signedAgreement);
            TempData["Success"] = "Contract updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (ApiClientException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound();
        }
        catch (ApiClientException ex)
        {
            _logger.LogError(ex, "Failed to update contract {ContractId}", id);
            ModelState.AddModelError(string.Empty, "Could not update the contract. Please try again.");
        }

        await SetClientDropdownAsync(contract.ClientId);
        return View(contract);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            var contract = await _apiClient.GetContractAsync(id.Value);
            if (contract == null)
            {
                return NotFound();
            }

            return View(contract);
        }
        catch (ApiClientException ex)
        {
            _logger.LogError(ex, "Failed to load contract {ContractId} for delete", id);
            TempData["Error"] = "Could not load the contract.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _apiClient.DeleteContractAsync(id);
            TempData["Success"] = "Contract deleted successfully.";
        }
        catch (ApiClientException ex)
        {
            _logger.LogError(ex, "Failed to delete contract {ContractId}", id);
            TempData["Error"] = "Could not delete the contract.";
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Download(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        try
        {
            var contract = await _apiClient.GetContractAsync(id.Value);
            if (contract == null || string.IsNullOrEmpty(contract.SignedAgreementFilePath))
            {
                return NotFound();
            }

            var (content, fileName, contentType) = await _apiClient.DownloadContractAsync(id.Value);
            return File(content, contentType, fileName);
        }
        catch (ApiClientException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            TempData["Error"] = "The signed agreement file could not be found.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (ApiClientException ex)
        {
            _logger.LogError(ex, "Failed to download contract {ContractId}", id);
            TempData["Error"] = "Could not download the signed agreement from the API.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    private async Task SetClientDropdownAsync(int? selectedClientId = null)
    {
        var clients = (await _apiClient.GetClientsAsync()).OrderBy(c => c.Name).ToList();
        SetClientDropdown(clients, selectedClientId);
    }

    private void SetClientDropdown(IReadOnlyList<Client> clients, int? selectedClientId = null)
    {
        ViewBag.ClientList = DropdownHelper.BuildClientList(clients, selectedClientId);
    }

    private void ClearNavigationPropertyValidation()
    {
        ModelState.Remove("Client");
        foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Client.", StringComparison.Ordinal)).ToList())
        {
            ModelState.Remove(key);
        }
    }

    private void TryBindClientIdFromForm(Contract contract)
    {
        ClearNavigationPropertyValidation();

        if (contract.ClientId > 0)
        {
            return;
        }

        if (int.TryParse(Request.Form["ClientId"], out var clientId) && clientId > 0)
        {
            contract.ClientId = clientId;
            ModelState.Remove(nameof(contract.ClientId));
        }
    }

    private static bool IsValidPdf(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName);
        return !string.IsNullOrWhiteSpace(extension)
            && AllowedPdfExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}
