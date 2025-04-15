using System.ComponentModel.DataAnnotations;

namespace ProductiveMachine.WebApp.Models;

public class TodoCategory
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(7)]
    public string? Color { get; set; } // Hex color code (e.g., #FF5733)
    
    public string? Icon { get; set; } // Icon identifier or name
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    // Foreign key
    public string UserId { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual ApplicationUser User { get; set; } = null!;
    
    public virtual ICollection<TodoItem> TodoItems { get; set; } = new List<TodoItem>();
} 