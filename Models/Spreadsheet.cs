using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;

namespace blazor_spreadsheet_agent.Models;

public class Spreadsheet
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string ContentType { get; set; } = string.Empty;
    
    [Required]
    public long FileSize { get; set; }
    
    [Required]
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    // Navigation property for related data rows
    public ICollection<SpreadsheetData> DataRows { get; set; } = new List<SpreadsheetData>();
    
    // Not mapped property to hold the count of data rows
    [NotMapped]
    public int DataRowsCount { get; set; }
}
