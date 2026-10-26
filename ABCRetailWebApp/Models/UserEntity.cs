using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCRetailWebApp.Models
{
    [Table("Users")]
    public class UserEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "Customer"; // Admin, Customer

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginDate { get; set; }

        // Navigation Properties
        public virtual ICollection<ShoppingCartEntity> CartItems { get; set; } = new List<ShoppingCartEntity>();
        public virtual ICollection<CustomerOrderEntity> Orders { get; set; } = new List<CustomerOrderEntity>();
    }
}