using System.ComponentModel.DataAnnotations;

namespace BackOfficeService.Models;

public class VisitorLog
{
    [Key]
    public int Id { get; set; }
    
    [MaxLength(100)]
    public string? Identity { get; set; } // IP or Email
    
    [MaxLength(255)]
    public string? PageUrl { get; set; }
    
    [MaxLength(255)]
    public string? SearchTerm { get; set; }
    
    public int? FlightId { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
