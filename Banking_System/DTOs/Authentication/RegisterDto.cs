using System.ComponentModel.DataAnnotations;

namespace Banking_System.DTOs.Authentication
{
    public class RegisterDto
    {
        [Required, StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Phone, StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
    }
}
