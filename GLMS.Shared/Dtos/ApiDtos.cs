using System.ComponentModel.DataAnnotations;
using GLMS.Shared.Models;

namespace GLMS.Shared.Dtos;

public class LoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class CreateContractDto
{
    [Range(1, int.MaxValue)]
    public int ClientId { get; set; }

    [Required]
    [StringLength(200)]
    public string ContractNumber { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Active;

    [Required]
    [StringLength(500)]
    public string ServiceLevel { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }
}

public class UpdateContractStatusDto
{
    [Required]
    public ContractStatus Status { get; set; }
}

public class CreateServiceRequestDto
{
    [Range(1, int.MaxValue)]
    public int ContractId { get; set; }

    [Required]
    public string RequestNumber { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal CostUSD { get; set; }

    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Pending;
    public string? Notes { get; set; }
}

public class ExchangeRateResponse
{
    public decimal Rate { get; set; }
    public DateTime Timestamp { get; set; }
}
