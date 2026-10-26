using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCRetailWebApp.Models
{
    [Table("CustomerOrders")]
    public class CustomerOrderEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(200)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [MaxLength(50)]
        public string OrderStatus { get; set; } = "Pending"; // Pending, Processing, Shipped, Delivered, Cancelled

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? DeliveryAddress { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual UserEntity User { get; set; } = null!;

        public virtual ICollection<OrderItemEntity> Items { get; set; } = new List<OrderItemEntity>();
    }
}