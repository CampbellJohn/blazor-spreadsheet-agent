using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace blazor_spreadsheet_agent.Models;

public class SpreadsheetData
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int SpreadsheetId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string SheetName { get; set; } = string.Empty;
    
    [Required]
    public int RowNumber { get; set; }
    
    [Required]
    [Column(TypeName = "nvarchar(MAX)")]
    public string JsonData { get; set; } = string.Empty;
    
    // Navigation property
    [ForeignKey("SpreadsheetId")]
    public Spreadsheet Spreadsheet { get; set; } = null!;
}
