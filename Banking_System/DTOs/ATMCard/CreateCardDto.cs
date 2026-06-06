using System.ComponentModel.DataAnnotations;

namespace Banking_System.DTOs.ATMCard
{
    public class CreateCardDto
    {
        [Required]
        public Guid BankAccountId { get; set; } // Linked account ID
    }
}
