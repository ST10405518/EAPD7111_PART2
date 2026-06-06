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
public class ContractsController : ControllerBase
{
    private static readonly string[] AllowedPdfExtensions = [".pdf"];
    private const string ContractUploadFolder = "uploads/contracts";

    private readonly IContractRepository _contractRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IFileUploadService _fileUploadService;
    private readonly IContractStatusAutomationService _statusAutomationService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly ILogger<ContractsController> _logger;

    public ContractsController(
        IContractRepository contractRepository,
        IClientRepository clientRepository,
        IFileUploadService fileUploadService,
        IContractStatusAutomationService statusAutomationService,
        IWebHostEnvironment webHostEnvironment,
        ILogger<ContractsController> logger)
    {
        _contractRepository = contractRepository;
        _clientRepository = clientRepository;
        _fileUploadService = fileUploadService;
        _statusAutomationService = statusAutomationService;
        _webHostEnvironment = webHostEnvironment;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Contract>>> GetAll(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] ContractStatus? status,
        CancellationToken cancellationToken)
    {
        await _statusAutomationService.ApplyAutomaticStatusUpdatesAsync(cancellationToken);

        var contracts = await _contractRepository.GetFilteredAsync(
            startDate,
            endDate,
            status,
            cancellationToken);

        return Ok(contracts);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Contract>> GetById(int id, CancellationToken cancellationToken)
    {
        var contract = await _contractRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (contract == null)
        {
            return NotFound(new { message = $"Contract {id} not found." });
        }

        return Ok(contract);
    }

    [HttpPost]
    [RequestSizeLimit(52_428_800)]
    public async Task<ActionResult<Contract>> Create(
        [FromForm] CreateContractDto dto,
        IFormFile? signedAgreement,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var dateRangeError = ContractValidationService.GetDateRangeErrorMessage(dto.StartDate, dto.EndDate);
        if (dateRangeError != null)
        {
            return BadRequest(new { message = dateRangeError });
        }

        if (!await _clientRepository.ExistsAsync(dto.ClientId, cancellationToken))
        {
            return BadRequest(new { message = $"Client {dto.ClientId} not found." });
        }

        var contract = new Contract
        {
            ClientId = dto.ClientId,
            ContractNumber = dto.ContractNumber,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = dto.Status,
            ServiceLevel = dto.ServiceLevel,
            Description = dto.Description,
            CreatedDate = DateTime.UtcNow
        };

        try
        {
            if (signedAgreement != null && signedAgreement.Length > 0)
            {
                if (!_fileUploadService.ValidateFile(signedAgreement, AllowedPdfExtensions))
                {
                    return BadRequest(new { message = "Only PDF files are allowed." });
                }

                var relativePath = await _fileUploadService.UploadFileAsync(signedAgreement, ContractUploadFolder);
                contract.SignedAgreementFilePath = "/" + relativePath;
            }

            contract.Status = _statusAutomationService.ResolveEffectiveStatus(contract.Status, contract.EndDate);

            var created = await _contractRepository.AddAsync(contract, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.ContractId }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create contract");
            return BadRequest(new { message = "Could not save the contract." });
        }
    }

    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<Contract>> UpdateStatus(
        int id,
        [FromBody] UpdateContractStatusDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var contract = await _contractRepository.GetByIdAsync(id, cancellationToken);
        if (contract == null)
        {
            return NotFound(new { message = $"Contract {id} not found." });
        }

        contract.Status = _statusAutomationService.ResolveEffectiveStatus(dto.Status, contract.EndDate);
        contract.ModifiedDate = DateTime.UtcNow;

        await _contractRepository.UpdateAsync(contract, cancellationToken);
        return Ok(contract);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var contract = await _contractRepository.GetByIdAsync(id, cancellationToken);
        if (contract == null)
        {
            return NotFound(new { message = $"Contract {id} not found." });
        }

        if (!string.IsNullOrEmpty(contract.SignedAgreementFilePath))
        {
            DeletePhysicalFile(contract.SignedAgreementFilePath);
        }

        await _contractRepository.DeleteAsync(contract, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id, CancellationToken cancellationToken)
    {
        var contract = await _contractRepository.GetByIdAsync(id, cancellationToken);
        if (contract == null || string.IsNullOrEmpty(contract.SignedAgreementFilePath))
        {
            return NotFound(new { message = "Contract or signed agreement not found." });
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
            return NotFound(new { message = "The signed agreement file could not be found on the server." });
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
}
