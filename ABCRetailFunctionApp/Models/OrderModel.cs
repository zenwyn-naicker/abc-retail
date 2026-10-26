namespace ABCRetailFunctionApp.Models
{
    public class OrderModel
    {
        public string RowKey { get; set; } = ""; 
        public string CustomerRowKey { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string ProductRowKey { get; set; } = "";
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; } = 1;
        public double UnitPrice { get; set; } = 0;
        public double TotalAmount => Quantity * UnitPrice;
        public string Status { get; set; } = "Pending";
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public string? DeliveryAddress { get; set; }
        public string? Notes { get; set; }
    }
}