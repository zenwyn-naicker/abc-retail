using Azure.Data.Tables;
using Azure;
using System.ComponentModel.DataAnnotations;

namespace ABCRetailWebApp.Models
{
    public class ProductEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; } = "";
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [Required]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = "";

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        [Display(Name = "Price")]
        public double Price { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Stock must be a positive number")]
        [Display(Name = "Stock Quantity")]
        public int Stock { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; } = "";

        [Display(Name = "Category")]
        public string Category { get; set; } = "";

        [Display(Name = "Image URL")]
        public string ImageUrl { get; set; } = "";

        [Display(Name = "Date Created")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}


