using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Banking_System.Models
{
    public class ATMCard
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid BankAccountId { get; set; }

        [Required]
        [StringLength(16)] // Normally , card numbers are 16 digits long
        public string CardNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string PinHash { get; set; } = string.Empty;

        public DateTime ExpiryDate { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("BankAccountId")]
        public BankAccount? BankAccount { get; set; }
    }
}
