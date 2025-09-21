using System.ComponentModel.DataAnnotations;

namespace AmplePack.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string EntityType { get; set; } = string.Empty; // "Order", "Inventory", "Customer", etc.
        
        public int EntityId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty; // "CREATE", "UPDATE", "DELETE", "STATUS_CHANGE", "ADD", "REMOVE"
        
        [StringLength(100)]
        public string Field { get; set; } = string.Empty; // Field that was changed (optional)
        
        public string? OldValue { get; set; } // Previous value (JSON or string)
        
        public string? NewValue { get; set; } // New value (JSON or string)
        
        public string? Details { get; set; } // Additional details (JSON)
        
        [Required]
        [StringLength(100)]
        public string ChangedBy { get; set; } = string.Empty; // User who made the change
        
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [StringLength(500)]
        public string? Reason { get; set; } // Optional reason for the change
        
        [StringLength(50)]
        public string? IpAddress { get; set; } // IP address of the user
        
        [StringLength(500)]
        public string? UserAgent { get; set; } // Browser/user agent info
    }
}