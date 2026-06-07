namespace Banking_System.DTOs.User
{
    public class UpdateProfileDto
    {

        public string? Address { get; set; } = string.Empty;


        public DateTime? DateOfBirth { get; set; }

        public string? CitizenId { get; set; } = string.Empty;

        public IFormFile? AvatarFile { get; set; }
    }
}
