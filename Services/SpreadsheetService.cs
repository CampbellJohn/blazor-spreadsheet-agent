using blazor_spreadsheet_agent.Data;
using blazor_spreadsheet_agent.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace blazor_spreadsheet_agent.Services;

public class SpreadsheetService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SpreadsheetService> _logger;

    public SpreadsheetService(ApplicationDbContext context, ILogger<SpreadsheetService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Spreadsheet> UploadSpreadsheetAsync(
        string fileName, 
        string contentType, 
        long fileSize, 
        string? description = null)
    {
        var spreadsheet = new Spreadsheet
        {
            FileName = fileName,
            ContentType = contentType,
            FileSize = fileSize,
            Description = description,
            UploadDate = DateTime.UtcNow
        };

        _context.Spreadsheets.Add(spreadsheet);
        await _context.SaveChangesAsync();
        
        return spreadsheet;
    }

    public async Task AddSpreadsheetDataAsync(int spreadsheetId, string sheetName, IEnumerable<Dictionary<string, object>> rows)
    {
        int rowNumber = 1;
        var dataRows = new List<SpreadsheetData>();

        foreach (var row in rows)
        {
            var jsonData = JsonSerializer.Serialize(row);
            dataRows.Add(new SpreadsheetData
            {
                SpreadsheetId = spreadsheetId,
                SheetName = sheetName,
                RowNumber = rowNumber++,
                JsonData = jsonData
            });
        }

        await _context.SpreadsheetData.AddRangeAsync(dataRows);
        await _context.SaveChangesAsync();
    }
    
    public async Task AddSpreadsheetDataBatchAsync(IEnumerable<SpreadsheetData> dataRows)
    {
        await _context.SpreadsheetData.AddRangeAsync(dataRows);
        await _context.SaveChangesAsync();
    }

    public async Task<Spreadsheet?> GetSpreadsheetAsync(int id)
    {
        return await _context.Spreadsheets
            .Include(s => s.DataRows)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<Spreadsheet>> GetSpreadsheetsAsync()
    {
        return await _context.Spreadsheets
            .Include(s => s.DataRows)
            .OrderByDescending(s => s.UploadDate)
            .Select(s => new Spreadsheet
            {
                Id = s.Id,
                FileName = s.FileName,
                ContentType = s.ContentType,
                FileSize = s.FileSize,
                UploadDate = s.UploadDate,
                Description = s.Description,
                DataRowsCount = s.DataRows.Count
            })
            .ToListAsync();
    }

    public async Task<bool> DeleteSpreadsheetAsync(int id)
    {
        var spreadsheet = await _context.Spreadsheets.FindAsync(id);
        if (spreadsheet == null)
            return false;

        _context.Spreadsheets.Remove(spreadsheet);
        await _context.SaveChangesAsync();
        return true;
    }
}
