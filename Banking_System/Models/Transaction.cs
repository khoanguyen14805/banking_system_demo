using Banking_System.Models.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Banking_System.Models
{
    public class Transaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid SourceAccountId { get; set; }

        // Allow null for transactions that are not transfers (e.g., deposits or withdrawals)
        public Guid? DestinationAccountId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public TransactionType Type { get; set; }

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public BankTransactionStatus Status { get; set; } = BankTransactionStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("SourceAccountId")]
        public BankAccount? SourceBankAccount { get; set; }

        [ForeignKey("DestinationAccountId")]
        public BankAccount? DestinationBankAccount { get; set; }
    }
}
