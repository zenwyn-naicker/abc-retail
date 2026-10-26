using Azure.Data.Tables;
using Azure;
using System.ComponentModel.DataAnnotations;

namespace ABCRetailWebApp.Models
{
    public class OrderEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "Order";
        public string RowKey { get; set; } = "";
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [Required(ErrorMessage = "Customer is required.")]
        [Display(Name = "Customer ID")]
        public string CustomerRowKey { get; set; } = "";

        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = "";

        [Required(ErrorMessage = "Product is required.")]
        [Display(Name = "Product ID")]
        public string ProductRowKey { get; set; } = "";

        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = "";

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; } = 1;

        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
        [Display(Name = "Unit Price")]
        public double UnitPrice { get; set; } = 0;

        [Display(Name = "Total Amount")]
        public double TotalAmount => Quantity * UnitPrice;

        [Display(Name = "Order Status")]
        public string Status { get; set; } = "Pending";

        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Delivery Address")]
        public string? DeliveryAddress { get; set; } = null;

        [Display(Name = "Order Notes")]
        public string? Notes { get; set; } = null;
    }
}
