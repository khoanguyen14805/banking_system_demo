using System.ComponentModel.DataAnnotations;

namespace Banking_System.DTOs.ATMCard
{
    public class CreateCardDto
    {
        [Required]
        public string AccountNumber { get; set; }
    }
}
