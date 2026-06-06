using Banking_System.Models.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Banking_System.Models
{
    public class BankAccount
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [StringLength(20)]
        public string AccountNumber { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Balance { get; set; } = 0;

        [Required]
        public AccountStatus Status { get; set; } = AccountStatus.Active;

        [Required]
        public AccountType AccountType { get; set; } = AccountType.Checking;

        [Required]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public User? User { get; set; }

        public ICollection<ATMCard> ATMCards { get; set; } = new List<ATMCard>();

        // Transactions where this account is the source (sent money)
        public ICollection<Transaction> SentTransactions { get; set; } = new List<Transaction>();

        // Transactions where this account is the destination (received money)
        public ICollection<Transaction> ReceivedTransactions { get; set; } = new List<Transaction>();
    }
}
