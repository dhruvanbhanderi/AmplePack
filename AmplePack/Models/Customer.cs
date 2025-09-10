using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AmplePack.Models
{
    public class Customer
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Contact { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public ICollection<Order> Orders { get; set; }
    }
}