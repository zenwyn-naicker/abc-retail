using System.ComponentModel.DataAnnotations;

namespace ABCRetailWebApp.Models
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Customer name is required")]
        [Display(Name = "Full Name")]
        [StringLength(100)]
        public string CustomerName { get; set; } = "";

        [Required(ErrorMessage = "Delivery address is required")]
        [Display(Name = "Delivery Address")]
        [StringLength(500)]
        public string DeliveryAddress { get; set; } = "";

        [Display(Name = "Order Notes")]
        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}