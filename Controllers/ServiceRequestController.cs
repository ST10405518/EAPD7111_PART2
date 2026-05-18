using EAPD7111_PART2.Data;
using EAPD7111_PART2.Helpers;
using EAPD7111_PART2.Models;
using EAPD7111_PART2.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EAPD7111_PART2.Controllers
{
    public class ServiceRequestController : Controller
    {
        private readonly GLMSDbContext _context;
        private readonly ICurrencyConversionService _currencyService;
        private readonly IContractWorkflowService _workflowService;
        private readonly IContractStatusAutomationService _statusAutomationService;

        public ServiceRequestController(
            GLMSDbContext context,
            ICurrencyConversionService currencyService,
            IContractWorkflowService workflowService,
            IContractStatusAutomationService statusAutomationService)
        {
            _context = context;
            _currencyService = currencyService;
            _workflowService = workflowService;
            _statusAutomationService = statusAutomationService;
        }

        public async Task<IActionResult> Index()
        {
            var serviceRequests = await _context.ServiceRequests
                .Include(s => s.Contract)
                .ThenInclude(c => c.Client)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();
            return View(serviceRequests);
        }

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

        public async Task<IActionResult> Create()
        {
            await SetContractDropdownAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ServiceRequestId,ContractId,RequestNumber,Description,CostUSD,Status,Notes")] ServiceRequest serviceRequest)
        {
            TryBindContractIdFromForm(serviceRequest);

            if (ModelState.IsValid)
            {
                var contract = await _context.Contracts.FindAsync(serviceRequest.ContractId);
                if (contract == null)
                {
                    ModelState.AddModelError("ContractId", "Contract not found.");
                    await SetContractDropdownAsync(serviceRequest.ContractId);
                    return View(serviceRequest);
                }

                var effectiveStatus = _statusAutomationService.ResolveEffectiveStatus(contract.Status, contract.EndDate);
                if (effectiveStatus != contract.Status)
                {
                    contract.Status = effectiveStatus;
                    contract.ModifiedDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                var blockedReason = _workflowService.GetServiceRequestBlockedReason(effectiveStatus);
                if (blockedReason != null)
                {
                    ModelState.AddModelError("ContractId", blockedReason);
                    await SetContractDropdownAsync(serviceRequest.ContractId);
                    return View(serviceRequest);
                }

                var exchangeRate = await _currencyService.GetUsdToZarRateAsync();
                serviceRequest.CostZAR = _currencyService.CalculateZARFromUSD(serviceRequest.CostUSD, exchangeRate);
                serviceRequest.CreatedDate = DateTime.UtcNow;

                try
                {
                    _context.Add(serviceRequest);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Service request created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError(string.Empty, "Could not save the service request. Check database connection.");
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

            var serviceRequest = await _context.ServiceRequests.FindAsync(id);
            if (serviceRequest == null)
            {
                return NotFound();
            }

            await SetContractDropdownAsync(serviceRequest.ContractId, activeOnly: false);
            return View(serviceRequest);
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
                    var contract = await _context.Contracts.FindAsync(serviceRequest.ContractId);
                    if (contract != null)
                    {
                        var blockedReason = _workflowService.GetServiceRequestBlockedReason(contract.Status);
                        if (blockedReason != null)
                        {
                            ModelState.AddModelError("ContractId", blockedReason);
                            await SetContractDropdownAsync(serviceRequest.ContractId, activeOnly: false);
                            return View(serviceRequest);
                        }
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

                    throw;
                }

                return RedirectToAction(nameof(Index));
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

        public async Task<IActionResult> GetExchangeRate()
        {
            var rate = await _currencyService.GetUsdToZarRateAsync();
            return Json(new { rate, timestamp = DateTime.UtcNow });
        }

        private async Task SetContractDropdownAsync(int? selectedContractId = null, bool activeOnly = true)
        {
            var query = _context.Contracts.Include(c => c.Client).AsQueryable();
            if (activeOnly)
            {
                query = query.Where(c => c.Status == ContractStatus.Active);
            }

            var contracts = await query.OrderBy(c => c.ContractNumber).ToListAsync();
            ViewBag.ContractList = DropdownHelper.BuildContractList(contracts, selectedContractId);
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

        private bool ServiceRequestExists(int id)
        {
            return _context.ServiceRequests.Any(e => e.ServiceRequestId == id);
        }
    }
}
