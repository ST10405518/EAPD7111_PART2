using EAPD7111_PART2.Data;
using EAPD7111_PART2.Helpers;
using EAPD7111_PART2.Models;
using EAPD7111_PART2.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAPD7111_PART2.Controllers
{
    public class ContractController : Controller
    {
        private static readonly string[] AllowedPdfExtensions = [".pdf"];
        private const string ContractUploadFolder = "uploads/contracts";

        private readonly GLMSDbContext _context;
        private readonly IFileUploadService _fileUploadService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IContractStatusAutomationService _statusAutomationService;
        private readonly ILogger<ContractController> _logger;

        public ContractController(
            GLMSDbContext context,
            IFileUploadService fileUploadService,
            IWebHostEnvironment webHostEnvironment,
            IContractStatusAutomationService statusAutomationService,
            ILogger<ContractController> logger)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _webHostEnvironment = webHostEnvironment;
            _statusAutomationService = statusAutomationService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, ContractStatus? status)
        {
            await _statusAutomationService.ApplyAutomaticStatusUpdatesAsync();

            var contracts = ContractQueryService.ApplyFilters(
                _context.Contracts.Include(c => c.Client),
                startDate,
                endDate,
                status);

            var model = await contracts.OrderByDescending(c => c.CreatedDate).ToListAsync();

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.Status = status;

            return View(model);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contract = await _context.Contracts
                .Include(c => c.Client)
                .Include(c => c.ServiceRequests)
                .FirstOrDefaultAsync(m => m.ContractId == id);

            if (contract == null)
            {
                return NotFound();
            }

            return View(contract);
        }

        public IActionResult Create()
        {
            var clients = _context.Clients.OrderBy(c => c.Name).ToList();
            if (clients.Count == 0)
            {
                TempData["Error"] = "You must create at least one client before adding a contract.";
                return RedirectToAction("Create", "Client");
            }

            SetClientDropdown();
            var contract = new Contract
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddYears(1),
                Status = ContractStatus.Active
            };
            return View(contract);
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

            if (!ModelState.IsValid)
            {
                SetClientDropdown(contract.ClientId);
                return View(contract);
            }

            try
            {
                if (signedAgreement != null && signedAgreement.Length > 0)
                {
                    if (!_fileUploadService.ValidateFile(signedAgreement, AllowedPdfExtensions))
                    {
                        ModelState.AddModelError("signedAgreement", "Only PDF files are allowed.");
                        SetClientDropdown(contract.ClientId);
                        return View(contract);
                    }

                    var relativePath = await _fileUploadService.UploadFileAsync(signedAgreement, ContractUploadFolder);
                    contract.SignedAgreementFilePath = "/" + relativePath;
                }

                contract.Status = _statusAutomationService.ResolveEffectiveStatus(contract.Status, contract.EndDate);
                contract.CreatedDate = DateTime.UtcNow;
                _context.Add(contract);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Contract created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("signedAgreement", ex.Message);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to save contract");
                ModelState.AddModelError(string.Empty, "Could not save the contract. Check that SQL Server is running and the database is migrated.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating contract");
                ModelState.AddModelError(string.Empty, $"Could not save the contract: {ex.Message}");
            }

            SetClientDropdown(contract.ClientId);
            return View(contract);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null)
            {
                return NotFound();
            }

            SetClientDropdown(contract.ClientId);
            return View(contract);
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

            if (!ModelState.IsValid)
            {
                SetClientDropdown(contract.ClientId);
                return View(contract);
            }

            try
            {
                if (signedAgreement != null && signedAgreement.Length > 0)
                {
                    if (!_fileUploadService.ValidateFile(signedAgreement, AllowedPdfExtensions))
                    {
                        ModelState.AddModelError("signedAgreement", "Only PDF files are allowed.");
                        SetClientDropdown(contract.ClientId);
                        return View(contract);
                    }

                    if (!string.IsNullOrEmpty(contract.SignedAgreementFilePath))
                    {
                        DeletePhysicalFile(contract.SignedAgreementFilePath);
                    }

                    var relativePath = await _fileUploadService.UploadFileAsync(signedAgreement, ContractUploadFolder);
                    contract.SignedAgreementFilePath = "/" + relativePath;
                }

                contract.Status = _statusAutomationService.ResolveEffectiveStatus(contract.Status, contract.EndDate);
                contract.ModifiedDate = DateTime.UtcNow;
                _context.Update(contract);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Contract updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("signedAgreement", ex.Message);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ContractExists(contract.ContractId))
                {
                    return NotFound();
                }

                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to update contract");
                ModelState.AddModelError(string.Empty, "Could not update the contract. Check database connection.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating contract");
                ModelState.AddModelError(string.Empty, $"Could not update the contract: {ex.Message}");
            }

            SetClientDropdown(contract.ClientId);
            return View(contract);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contract = await _context.Contracts
                .Include(c => c.Client)
                .FirstOrDefaultAsync(m => m.ContractId == id);
            if (contract == null)
            {
                return NotFound();
            }

            return View(contract);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract != null)
            {
                if (!string.IsNullOrEmpty(contract.SignedAgreementFilePath))
                {
                    DeletePhysicalFile(contract.SignedAgreementFilePath);
                }

                _context.Contracts.Remove(contract);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Contract deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Download(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null || string.IsNullOrEmpty(contract.SignedAgreementFilePath))
            {
                return NotFound();
            }

            var relativePath = contract.SignedAgreementFilePath.TrimStart('/');
            try
            {
                var fileBytes = _fileUploadService.DownloadFile(relativePath);
                var fileName = $"Contract_{contract.ContractNumber}.pdf";
                return File(fileBytes, "application/pdf", fileName);
            }
            catch (FileNotFoundException)
            {
                TempData["Error"] = "The signed agreement file could not be found on the server.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        private void DeletePhysicalFile(string webRelativePath)
        {
            var webRoot = _webHostEnvironment.WebRootPath
                ?? Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
            var filePath = Path.Combine(webRoot, webRelativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        private bool ContractExists(int id)
        {
            return _context.Contracts.Any(e => e.ContractId == id);
        }

        private void SetClientDropdown(int? selectedClientId = null)
        {
            var clients = _context.Clients.OrderBy(c => c.Name).ToList();
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
    }
}
