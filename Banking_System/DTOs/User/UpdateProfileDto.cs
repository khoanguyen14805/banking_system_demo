using System.ComponentModel.DataAnnotations;

namespace Banking_System.DTOs.User
{
    public class UpdateProfileDto
    {
        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string CitizenId { get; set; } = string.Empty;

        public IFormFile? AvatarFile { get; set; }
    }
}
