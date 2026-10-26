using Azure.Data.Tables;
using Azure;
using System.ComponentModel.DataAnnotations;

namespace ABCRetailWebApp.Models
{
    public class CustomerEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "Customer";
        public string RowKey { get; set; } = "";
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [Required]
        [Display(Name = "Customer Name")]
        public string Name { get; set; } = "";

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = "";

        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = "";

        [Display(Name = "Address")]
        public string Address { get; set; } = "";

        [Display(Name = "Date Created")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}