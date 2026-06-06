namespace Banking_System.DTOs.Transaction
{
    public class TransactionResponseDto
    {
        public Guid Id { get; set; }
        public string SourceAccountNumber { get; set; } = "N/A";
        public string DestinationAccountNumber { get; set; } = "N/A";
        public decimal Amount { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
