using System.Globalization;
using System.Text;
using CsvHelper;
using ExcelDataReader;
using blazor_spreadsheet_agent.Models;
using Microsoft.Extensions.Logging;

namespace blazor_spreadsheet_agent.Services;

public class FileProcessingService
{
    private readonly ILogger<FileProcessingService> _logger;
    private readonly SpreadsheetService _spreadsheetService;
    private const int BatchSize = 100; // Process files in batches to manage memory

    public FileProcessingService(
        ILogger<FileProcessingService> logger,
        SpreadsheetService spreadsheetService)
    {
        _logger = logger;
        _spreadsheetService = spreadsheetService;
    }

    public async Task<bool> ProcessCsvFileAsync(Stream fileStream, int spreadsheetId, bool hasHeaders = true)
    {
        try
        {
            using var reader = new StreamReader(fileStream, Encoding.UTF8);
            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = hasHeaders,
                Encoding = Encoding.UTF8,
                BadDataFound = context => _logger.LogWarning($"Bad data found: {context.Field}")
            };
            
            using var csv = new CsvReader(reader, config);

            var batch = new List<Dictionary<string, object>>();
            var headers = new List<string>();
            var isFirstRow = true;

            while (await csv.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                
                if (isFirstRow && hasHeaders)
                {
                    // Get headers from the first row
                    csv.ReadHeader();
                    headers = csv.HeaderRecord?.ToList() ?? new List<string>();
                    
                    // If no headers, generate default ones (Column1, Column2, etc.)
                    if (headers.Count == 0 || headers.All(string.IsNullOrEmpty))
                    {
                        headers = Enumerable.Range(1, csv.Parser.Count).Select(i => $"Column{i}").ToList();
                    }
                    isFirstRow = false;
                    
                    // Skip to next record if we're using headers
                    await csv.ReadAsync();
                }
                
                // If we still don't have headers (no headers in file), generate them
                if (headers.Count == 0)
                {
                    headers = Enumerable.Range(1, csv.Parser.Count).Select(i => $"Column{i}").ToList();
                }

                
                // Process each field in the row
                for (int i = 0; i < headers.Count; i++)
                {
                    var header = headers[i];
                    var value = csv.GetField(i);
                    row[header] = value is not null ? (object)value : DBNull.Value;
                }
                
                batch.Add(row);
                
                // Process in batches to manage memory
                if (batch.Count >= BatchSize)
                {
                    await ProcessBatchAsync(batch, spreadsheetId, "Sheet1");
                    batch.Clear();
                }
            }
            
            // Process any remaining records in the last batch
            if (batch.Count > 0)
            {
                await ProcessBatchAsync(batch, spreadsheetId, "Sheet1");
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CSV file for spreadsheet {SpreadsheetId}", spreadsheetId);
            throw;
        }
    }

    public async Task<bool> ProcessExcelFileAsync(Stream fileStream, int spreadsheetId, bool hasHeaders = true)
    {
        // Required for ExcelDataReader to work with .NET Core
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        
        using var reader = ExcelReaderFactory.CreateReader(fileStream);
        int sheetIndex = 0;
        
        do
        {
            string sheetName = reader.Name ?? $"Sheet{sheetIndex + 1}";
            
            var batch = new List<Dictionary<string, object>>();
            var headers = new List<string>();
            var rowIndex = 0;
            
            // Read each row in the current sheet
            while (reader.Read())
            {
                var row = new Dictionary<string, object>();
                
                // Process headers if first row and hasHeaders is true
                if (rowIndex == 0 && hasHeaders)
                {
                    // Read header row
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var header = reader.GetValue(i)?.ToString() ?? $"Column{i + 1}";
                        headers.Add(header);
                    }
                    rowIndex++;
                    continue;
                }
                
                // If no headers, generate them
                if (headers.Count == 0)
                {
                    headers = Enumerable.Range(1, reader.FieldCount).Select(i => $"Column{i}").ToList();
                }
                
                // Process data row
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var header = i < headers.Count ? headers[i] : $"Column{i + 1}";
                    var value = reader.GetValue(i);
                    row[header] = value ?? DBNull.Value;
                }
                
                batch.Add(row);
                rowIndex++;
                
                // Process in batches to manage memory
                if (batch.Count >= BatchSize)
                {
                    await ProcessBatchAsync(batch, spreadsheetId, sheetName);
                    batch.Clear();
                }
            }
            
            // Process any remaining records in the last batch
            if (batch.Count > 0)
            {
                await ProcessBatchAsync(batch, spreadsheetId, sheetName);
            }
            
            sheetIndex++;
            
        } while (reader.NextResult()); // Move to next sheet
        
        return true;
    }
    
    private async Task ProcessBatchAsync(List<Dictionary<string, object>> batch, int spreadsheetId, string sheetName)
    {
        try
        {
            var dataRows = batch.Select((row, index) => new SpreadsheetData
            {
                SpreadsheetId = spreadsheetId,
                SheetName = sheetName,
                RowNumber = index + 1,
                JsonData = System.Text.Json.JsonSerializer.Serialize(row)
            }).ToList();
            
            await _spreadsheetService.AddSpreadsheetDataBatchAsync(dataRows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch for spreadsheet {SpreadsheetId}, sheet {SheetName}", 
                spreadsheetId, sheetName);
            throw;
        }
    }
}
