using System.ComponentModel.DataAnnotations;

namespace Banking_System.DTOs.Transaction
{
    public class TransferDto
    {
        [Required]
        public Guid FromAccountId { get; set; }

        [Required]
        public Guid ToAccountId { get; set; }

        [Required]
        [Range(10000, 1000000000, ErrorMessage = "The minimum transaction amount is 10,000 VND.")]
        public decimal Amount { get; set; }

        [StringLength(200)]
        public string Description { get; set; } = string.Empty;
    }
}
