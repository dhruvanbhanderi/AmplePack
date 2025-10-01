using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AmplePack.Models
{
    public class Order
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Customer is required")]
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }
        
        [Required(ErrorMessage = "Order date is required")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }
        
        [Required(ErrorMessage = "Status is required")]
        [StringLength(50, ErrorMessage = "Status cannot be longer than 50 characters")]
        public string Status { get; set; } = "Pending";
        
        [Required(ErrorMessage = "Total amount is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Total amount must be a positive number")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }
        
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }

    public class ChangeStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}