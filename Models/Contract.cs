using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EAPD7111_PART2.Models
{
    public class Contract
    {
        [Key]
        public int ContractId { get; set; }

        [Display(Name = "Client")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a client from the list.")]
        public int ClientId { get; set; }

        [Required]
        [StringLength(200)]
        public string ContractNumber { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public ContractStatus Status { get; set; } = ContractStatus.Draft;

        [Required]
        [StringLength(500)]
        public string ServiceLevel { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(255)]
        public string? SignedAgreementFilePath { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        [ForeignKey("ClientId")]
        [ValidateNever]
        public Client? Client { get; set; }

        public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
    }
}
