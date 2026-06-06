using Banking_System.Models.Enum;
using System.ComponentModel.DataAnnotations;

namespace Banking_System.DTOs.BankAccount
{
    public class CreateAccountDto
    {
        [Required]
        [StringLength(20)]
        public AccountType AccountType { get; set; } = AccountType.Checking; // Default type
    }
}
