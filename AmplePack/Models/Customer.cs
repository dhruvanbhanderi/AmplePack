using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AmplePack.Models
{
    public class Customer
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Customer name is required")]
        [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(50, ErrorMessage = "Contact cannot be longer than 50 characters")]
        public string Contact { get; set; } = string.Empty;
        
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(100, ErrorMessage = "Email cannot be longer than 100 characters")]
        public string Email { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "Address cannot be longer than 500 characters")]
        public string Address { get; set; } = string.Empty;
        
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<CustomerProduct> CustomerProducts { get; set; } = new List<CustomerProduct>();
    }
}