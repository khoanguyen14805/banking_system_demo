namespace Banking_System.DTOs.ATMCard
{
    public class CardResponseDto
    {
        public Guid Id { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public string AssociatedAccountNumber { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        //Do not return PIN Code to secure customer info
    }
}
