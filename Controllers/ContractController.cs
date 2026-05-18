using EAPD7111_PART2.Data;
using EAPD7111_PART2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAPD7111_PART2.Controllers
{
    public class ContractController : Controller
    {
        private readonly GLMSDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ContractController(GLMSDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Contract
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, ContractStatus? status)
        {
            var contracts = _context.Contracts.Include(c => c.Client).AsQueryable();

            // Filter by date range
            if (startDate.HasValue)
            {
                contracts = contracts.Where(c => c.StartDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                contracts = contracts.Where(c => c.EndDate <= endDate.Value);
            }

            // Filter by status
            if (status.HasValue)
            {
                contracts = contracts.Where(c => c.Status == status.Value);
            }

            var model = await contracts.OrderByDescending(c => c.CreatedDate).ToListAsync();

            // Pass filter values to view for maintaining state
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.Status = status;

            return View(model);
        }

        // GET: Contract/Details/5
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

        // GET: Contract/Create
        public IActionResult Create()
        {
            ViewBag.Clients = _context.Clients.ToList();
            return View();
        }

        // POST: Contract/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ContractId,ClientId,ContractNumber,StartDate,EndDate,Status,ServiceLevel,Description")] Contract contract, IFormFile? signedAgreement)
        {
            if (ModelState.IsValid)
            {
                // Handle file upload
                if (signedAgreement != null && signedAgreement.Length > 0)
                {
                    // Validate file type (only PDF allowed)
                    if (signedAgreement.ContentType != "application/pdf" && 
                        !signedAgreement.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("signedAgreement", "Only PDF files are allowed.");
                        ViewBag.Clients = _context.Clients.ToList();
                        return View(contract);
                    }

                    // Save file
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "contracts");
                    Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + signedAgreement.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await signedAgreement.CopyToAsync(fileStream);
                    }

                    contract.SignedAgreementFilePath = "/uploads/contracts/" + uniqueFileName;
                }

                contract.CreatedDate = DateTime.UtcNow;
                _context.Add(contract);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Clients = _context.Clients.ToList();
            return View(contract);
        }

        // GET: Contract/Edit/5
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

        // POST: Contract/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ContractId,ClientId,ContractNumber,StartDate,EndDate,Status,ServiceLevel,Description,SignedAgreementFilePath,CreatedDate")] Contract contract, IFormFile? signedAgreement)
        {
            if (id != contract.ContractId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle file upload if new file provided
                    if (signedAgreement != null && signedAgreement.Length > 0)
                    {
                        // Validate file type (only PDF allowed)
                        if (signedAgreement.ContentType != "application/pdf" && 
                            !signedAgreement.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            ModelState.AddModelError("signedAgreement", "Only PDF files are allowed.");
                            ViewBag.Clients = _context.Clients.ToList();
                            return View(contract);
                        }

                        // Delete old file if exists
                        if (!string.IsNullOrEmpty(contract.SignedAgreementFilePath))
                        {
                            string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, contract.SignedAgreementFilePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        // Save new file
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "contracts");
                        Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + signedAgreement.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await signedAgreement.CopyToAsync(fileStream);
                        }

                        contract.SignedAgreementFilePath = "/uploads/contracts/" + uniqueFileName;
                    }

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
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Clients = _context.Clients.ToList();
            return View(contract);
        }

        // GET: Contract/Delete/5
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

        // POST: Contract/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract != null)
            {
                // Delete file if exists
                if (!string.IsNullOrEmpty(contract.SignedAgreementFilePath))
                {
                    string filePath = Path.Combine(_webHostEnvironment.WebRootPath, contract.SignedAgreementFilePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.Contracts.Remove(contract);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Contract/Download/5
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

            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, contract.SignedAgreementFilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            string fileName = Path.GetFileName(filePath);
            return File(fileBytes, "application/pdf", fileName);
        }

        private bool ContractExists(int id)
        {
            return _context.Contracts.Any(e => e.ContractId == id);
        }
    }
}
