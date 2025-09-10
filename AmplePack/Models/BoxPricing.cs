namespace AmplePack.Models
{
    public class BoxPricing
    {
        public int Id { get; set; }
        public decimal L { get; set; }
        public decimal W { get; set; }
        public decimal H { get; set; }
        public int GSM { get; set; }
        public string PrintingType { get; set; }
        public int Quantity { get; set; }
        public decimal Cost { get; set; }
    }
}