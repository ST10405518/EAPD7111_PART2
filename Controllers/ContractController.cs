using EAPD7111_PART2.Data;
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

        public ContractController(
            GLMSDbContext context,
            IFileUploadService fileUploadService,
            IWebHostEnvironment webHostEnvironment,
            IContractStatusAutomationService statusAutomationService)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _webHostEnvironment = webHostEnvironment;
            _statusAutomationService = statusAutomationService;
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
            ViewBag.Clients = _context.Clients.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ContractId,ClientId,ContractNumber,StartDate,EndDate,Status,ServiceLevel,Description")] Contract contract, IFormFile? signedAgreement)
        {
            var dateRangeError = ContractValidationService.GetDateRangeErrorMessage(contract.StartDate, contract.EndDate);
            if (dateRangeError != null)
            {
                ModelState.AddModelError(nameof(contract.EndDate), dateRangeError);
            }

            if (ModelState.IsValid)
            {
                if (signedAgreement != null && signedAgreement.Length > 0)
                {
                    if (!_fileUploadService.ValidateFile(signedAgreement, AllowedPdfExtensions))
                    {
                        ModelState.AddModelError("signedAgreement", "Only PDF files are allowed.");
                        ViewBag.Clients = _context.Clients.ToList();
                        return View(contract);
                    }

                    var relativePath = await _fileUploadService.UploadFileAsync(signedAgreement, ContractUploadFolder);
                    contract.SignedAgreementFilePath = "/" + relativePath.Replace('\\', '/');
                }

                contract.CreatedDate = DateTime.UtcNow;
                _context.Add(contract);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Clients = _context.Clients.ToList();
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

            ViewBag.Clients = _context.Clients.ToList();
            return View(contract);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ContractId,ClientId,ContractNumber,StartDate,EndDate,Status,ServiceLevel,Description,SignedAgreementFilePath,CreatedDate")] Contract contract, IFormFile? signedAgreement)
        {
            if (id != contract.ContractId)
            {
                return NotFound();
            }

            var dateRangeError = ContractValidationService.GetDateRangeErrorMessage(contract.StartDate, contract.EndDate);
            if (dateRangeError != null)
            {
                ModelState.AddModelError(nameof(contract.EndDate), dateRangeError);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (signedAgreement != null && signedAgreement.Length > 0)
                    {
                        if (!_fileUploadService.ValidateFile(signedAgreement, AllowedPdfExtensions))
                        {
                            ModelState.AddModelError("signedAgreement", "Only PDF files are allowed.");
                            ViewBag.Clients = _context.Clients.ToList();
                            return View(contract);
                        }

                        if (!string.IsNullOrEmpty(contract.SignedAgreementFilePath))
                        {
                            DeletePhysicalFile(contract.SignedAgreementFilePath);
                        }

                        var relativePath = await _fileUploadService.UploadFileAsync(signedAgreement, ContractUploadFolder);
                        contract.SignedAgreementFilePath = "/" + relativePath.Replace('\\', '/');
                    }

                    contract.Status = _statusAutomationService.ResolveEffectiveStatus(contract.Status, contract.EndDate);
                    contract.ModifiedDate = DateTime.UtcNow;
                    _context.Update(contract);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContractExists(contract.ContractId))
                    {
                        return NotFound();
                    }

                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Clients = _context.Clients.ToList();
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
            }

            await _context.SaveChangesAsync();
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
                var fileName = Path.GetFileName(relativePath);
                return File(fileBytes, "application/pdf", fileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound();
            }
        }

        private void DeletePhysicalFile(string webRelativePath)
        {
            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, webRelativePath.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        private bool ContractExists(int id)
        {
            return _context.Contracts.Any(e => e.ContractId == id);
        }
    }
}
