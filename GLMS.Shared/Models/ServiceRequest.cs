using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GLMS.Shared.Models;

public class ServiceRequest
{
    [Key]
    public int ServiceRequestId { get; set; }

    [Display(Name = "Contract")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a contract from the list.")]
    public int ContractId { get; set; }

    [Required]
    [StringLength(200)]
    public string RequestNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0, double.MaxValue)]
    public decimal CostUSD { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal CostZAR { get; set; }

    [Required]
    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Pending;

    [StringLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    [ForeignKey("ContractId")]
    [ValidateNever]
    public Contract? Contract { get; set; }
}
