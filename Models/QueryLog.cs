using System.ComponentModel.DataAnnotations;

namespace blazor_spreadsheet_agent.Models;

public class QueryLog
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [Required]
    [MaxLength(1000)]
    public string NaturalLanguageQuery { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(4000)]
    public string GeneratedSql { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Error { get; set; }
    
    [Required]
    public bool WasSuccessful { get; set; }
    
    public int? RowsReturned { get; set; }
    
    [MaxLength(100)]
    public string? UserId { get; set; }  // Could be tied to ASP.NET Core Identity if implemented
    
    [MaxLength(50)]
    public string? ClientIp { get; set; }
}
