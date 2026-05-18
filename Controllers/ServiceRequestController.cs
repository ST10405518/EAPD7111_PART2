using EAPD7111_PART2.Data;
using EAPD7111_PART2.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;

namespace EAPD7111_PART2.Controllers
{
    public class ServiceRequestController : Controller
    {
        private readonly GLMSDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ServiceRequestController> _logger;

        public ServiceRequestController(GLMSDbContext context, IHttpClientFactory httpClientFactory, ILogger<ServiceRequestController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // GET: ServiceRequest
        public async Task<IActionResult> Index()
        {
            var serviceRequests = await _context.ServiceRequests
                .Include(s => s.Contract)
                .ThenInclude(c => c.Client)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();
            return View(serviceRequests);
        }

        // GET: ServiceRequest/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceRequest = await _context.ServiceRequests
                .Include(s => s.Contract)
                .ThenInclude(c => c.Client)
                .FirstOrDefaultAsync(m => m.ServiceRequestId == id);

            if (serviceRequest == null)
            {
                return NotFound();
            }

            return View(serviceRequest);
        }

        // GET: ServiceRequest/Create
        public async Task<IActionResult> Create()
        {
            // Only show active contracts
            ViewBag.Contracts = await _context.Contracts
                .Include(c => c.Client)
                .Where(c => c.Status == ContractStatus.Active)
                .ToListAsync();
            return View();
        }

        // POST: ServiceRequest/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ServiceRequestId,ContractId,RequestNumber,Description,CostUSD,Status,Notes")] ServiceRequest serviceRequest)
        {
            if (ModelState.IsValid)
            {
                // Validate contract status - cannot create service request for expired or on-hold contracts
                var contract = await _context.Contracts.FindAsync(serviceRequest.ContractId);
                if (contract == null)
                {
                    ModelState.AddModelError("ContractId", "Contract not found.");
                    ViewBag.Contracts = await _context.Contracts.Include(c => c.Client).Where(c => c.Status == ContractStatus.Active).ToListAsync();
                    return View(serviceRequest);
                }

                if (contract.Status == ContractStatus.Expired || contract.Status == ContractStatus.OnHold)
                {
                    ModelState.AddModelError("ContractId", $"Cannot create service request for contract with status: {contract.Status}. Only Active contracts are allowed.");
                    ViewBag.Contracts = await _context.Contracts.Include(c => c.Client).Where(c => c.Status == ContractStatus.Active).ToListAsync();
                    return View(serviceRequest);
                }

                // Get exchange rate from API
                decimal exchangeRate = await GetExchangeRateAsync();
                if (exchangeRate <= 0)
                {
                    ModelState.AddModelError("", "Unable to retrieve exchange rate. Please try again.");
                    ViewBag.Contracts = await _context.Contracts.Include(c => c.Client).Where(c => c.Status == ContractStatus.Active).ToListAsync();
                    return View(serviceRequest);
                }

                // Calculate ZAR cost
                serviceRequest.CostZAR = serviceRequest.CostUSD * exchangeRate;
                serviceRequest.CreatedDate = DateTime.UtcNow;

                _context.Add(serviceRequest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Contracts = await _context.Contracts.Include(c => c.Client).Where(c => c.Status == ContractStatus.Active).ToListAsync();
            return View(serviceRequest);
        }

        // GET: ServiceRequest/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceRequest = await _context.ServiceRequests.FindAsync(id);
            if (serviceRequest == null)
            {
                return NotFound();
            }

            ViewBag.Contracts = await _context.Contracts.Include(c => c.Client).ToListAsync();
            return View(serviceRequest);
        }

        // POST: ServiceRequest/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ServiceRequestId,ContractId,RequestNumber,Description,CostUSD,CostZAR,Status,Notes,CreatedDate")] ServiceRequest serviceRequest)
        {
            if (id != serviceRequest.ServiceRequestId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Validate contract status
                    var contract = await _context.Contracts.FindAsync(serviceRequest.ContractId);
                    if (contract != null && (contract.Status == ContractStatus.Expired || contract.Status == ContractStatus.OnHold))
                    {
                        ModelState.AddModelError("ContractId", $"Cannot update service request for contract with status: {contract.Status}.");
                        ViewBag.Contracts = await _context.Contracts.Include(c => c.Client).ToListAsync();
                        return View(serviceRequest);
                    }

                    serviceRequest.ModifiedDate = DateTime.UtcNow;
                    _context.Update(serviceRequest);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ServiceRequestExists(serviceRequest.ServiceRequestId))
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

            ViewBag.Contracts = await _context.Contracts.Include(c => c.Client).ToListAsync();
            return View(serviceRequest);
        }

        // GET: ServiceRequest/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var serviceRequest = await _context.ServiceRequests
                .Include(s => s.Contract)
                .ThenInclude(c => c.Client)
                .FirstOrDefaultAsync(m => m.ServiceRequestId == id);

            if (serviceRequest == null)
            {
                return NotFound();
            }

            return View(serviceRequest);
        }

        // POST: ServiceRequest/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var serviceRequest = await _context.ServiceRequests.FindAsync(id);
            if (serviceRequest != null)
            {
                _context.ServiceRequests.Remove(serviceRequest);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: ServiceRequest/GetExchangeRate
        public async Task<IActionResult> GetExchangeRate()
        {
            decimal rate = await GetExchangeRateAsync();
            return Json(new { rate = rate, timestamp = DateTime.UtcNow });
        }

        private async Task<decimal> GetExchangeRateAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("User-Agent", "GLMS/1.0");
                
                // Using a free exchange rate API (ExchangeRate-API)
                // Alternative: https://open.er-api.com/v6/latest/USD
                var response = await client.GetAsync("https://open.er-api.com/v6/latest/USD");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    // Parse JSON response
                    using var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
                    var rates = jsonDoc.RootElement.GetProperty("rates");
                    
                    if (rates.TryGetProperty("ZAR", out var zarRate))
                    {
                        return zarRate.GetDecimal();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching exchange rate");
            }

            // Fallback rate if API fails
            return 18.50m; // Default fallback rate
        }

        private bool ServiceRequestExists(int id)
        {
            return _context.ServiceRequests.Any(e => e.ServiceRequestId == id);
        }
    }
}
