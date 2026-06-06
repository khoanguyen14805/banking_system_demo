using System.ComponentModel.DataAnnotations;

namespace Banking_System.DTOs.Transaction
{
    public class DepositWithdrawDto
    {
        [Required]
        public Guid AccountId { get; set; }

        [Required]
        [Range(10000, 1000000000, ErrorMessage = "The minimum transaction amount is 10,000 VND and the maximum is 1 billion VND.")]
        public decimal Amount { get; set; }
    }
}
