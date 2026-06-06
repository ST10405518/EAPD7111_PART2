using GLMS.Api.Repositories;
using GLMS.Api.Services;
using GLMS.Shared.Dtos;
using GLMS.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServiceRequestsController : ControllerBase
{
    private readonly IServiceRequestRepository _serviceRequestRepository;
    private readonly IContractRepository _contractRepository;
    private readonly ICurrencyConversionService _currencyService;
    private readonly IContractWorkflowService _workflowService;
    private readonly IContractStatusAutomationService _statusAutomationService;

    public ServiceRequestsController(
        IServiceRequestRepository serviceRequestRepository,
        IContractRepository contractRepository,
        ICurrencyConversionService currencyService,
        IContractWorkflowService workflowService,
        IContractStatusAutomationService statusAutomationService)
    {
        _serviceRequestRepository = serviceRequestRepository;
        _contractRepository = contractRepository;
        _currencyService = currencyService;
        _workflowService = workflowService;
        _statusAutomationService = statusAutomationService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceRequest>>> GetAll(CancellationToken cancellationToken)
    {
        var serviceRequests = await _serviceRequestRepository.GetAllAsync(cancellationToken);
        return Ok(serviceRequests);
    }

    [HttpGet("exchange-rate")]
    public async Task<ActionResult<ExchangeRateResponse>> GetExchangeRate(CancellationToken cancellationToken)
    {
        var rate = await _currencyService.GetUsdToZarRateAsync();
        return Ok(new ExchangeRateResponse
        {
            Rate = rate,
            Timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ServiceRequest>> GetById(int id, CancellationToken cancellationToken)
    {
        var serviceRequest = await _serviceRequestRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (serviceRequest == null)
        {
            return NotFound(new { message = $"Service request {id} not found." });
        }

        return Ok(serviceRequest);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceRequest>> Create(
        [FromBody] CreateServiceRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var contract = await _contractRepository.GetByIdAsync(dto.ContractId, cancellationToken);
        if (contract == null)
        {
            return BadRequest(new { message = "Contract not found." });
        }

        var effectiveStatus = _statusAutomationService.ResolveEffectiveStatus(contract.Status, contract.EndDate);
        if (effectiveStatus != contract.Status)
        {
            contract.Status = effectiveStatus;
            contract.ModifiedDate = DateTime.UtcNow;
            await _contractRepository.UpdateAsync(contract, cancellationToken);
        }

        var blockedReason = _workflowService.GetServiceRequestBlockedReason(effectiveStatus);
        if (blockedReason != null)
        {
            return BadRequest(new { message = blockedReason });
        }

        var exchangeRate = await _currencyService.GetUsdToZarRateAsync();
        var serviceRequest = new ServiceRequest
        {
            ContractId = dto.ContractId,
            RequestNumber = dto.RequestNumber,
            Description = dto.Description,
            CostUSD = dto.CostUSD,
            CostZAR = _currencyService.CalculateZARFromUSD(dto.CostUSD, exchangeRate),
            Status = dto.Status,
            Notes = dto.Notes,
            CreatedDate = DateTime.UtcNow
        };

        var created = await _serviceRequestRepository.AddAsync(serviceRequest, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.ServiceRequestId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ServiceRequest>> Update(
        int id,
        [FromBody] ServiceRequest serviceRequest,
        CancellationToken cancellationToken)
    {
        if (id != serviceRequest.ServiceRequestId)
        {
            return BadRequest(new { message = "Route id does not match service request id." });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (!await _serviceRequestRepository.ExistsAsync(id, cancellationToken))
        {
            return NotFound(new { message = $"Service request {id} not found." });
        }

        var contract = await _contractRepository.GetByIdAsync(serviceRequest.ContractId, cancellationToken);
        if (contract == null)
        {
            return BadRequest(new { message = "Contract not found." });
        }

        var blockedReason = _workflowService.GetServiceRequestBlockedReason(contract.Status);
        if (blockedReason != null)
        {
            return BadRequest(new { message = blockedReason });
        }

        var exchangeRate = await _currencyService.GetUsdToZarRateAsync();
        serviceRequest.CostZAR = _currencyService.CalculateZARFromUSD(serviceRequest.CostUSD, exchangeRate);
        serviceRequest.ModifiedDate = DateTime.UtcNow;

        await _serviceRequestRepository.UpdateAsync(serviceRequest, cancellationToken);
        return Ok(serviceRequest);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var serviceRequest = await _serviceRequestRepository.GetByIdAsync(id, cancellationToken);
        if (serviceRequest == null)
        {
            return NotFound(new { message = $"Service request {id} not found." });
        }

        await _serviceRequestRepository.DeleteAsync(serviceRequest, cancellationToken);
        return NoContent();
    }
}
