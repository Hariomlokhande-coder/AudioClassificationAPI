using System;
using System.ComponentModel.DataAnnotations;

namespace AudioClassificationAPI.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        
        public required string Username { get; set; }
        
        [EmailAddress]
        public required string Email { get; set; }
        
        public required string PasswordHash { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
