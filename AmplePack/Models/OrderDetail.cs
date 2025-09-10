namespace AmplePack.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; }
        public string BoxType { get; set; }
        public string Size { get; set; } // Format: LxWxH
        public int Quantity { get; set; }
        public decimal PricePerBox { get; set; }
    }
}