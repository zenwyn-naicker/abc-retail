using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCRetailWebApp.Models
{
    [Table("ShoppingCart")]
    public class ShoppingCartEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CartId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ProductRowKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        public int Quantity { get; set; } = 1;

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [Required]
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        // Computed property for total price
        [NotMapped]
        public decimal TotalPrice => UnitPrice * Quantity;

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual UserEntity User { get; set; } = null!;
    }
}