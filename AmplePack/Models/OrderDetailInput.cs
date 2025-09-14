namespace AmplePack.Models
{
    public class OrderDetailInput
    {
        public int? CustomerProductId { get; set; }
        public string BoxType { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal PricePerBox { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? Notes { get; set; }
    }
}