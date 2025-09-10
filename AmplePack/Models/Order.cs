using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AmplePack.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public Customer Customer { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public ICollection<OrderDetail> OrderDetails { get; set; }
    }
}