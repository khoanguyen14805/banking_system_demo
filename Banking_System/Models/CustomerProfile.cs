using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Banking_System.Models
{
    public class CustomerProfile
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }


        [StringLength(255)]
        public string? Address { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(20)]
        public string? CitizenId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? AvatarUrl { get; set; }

        // Navigation property
        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
