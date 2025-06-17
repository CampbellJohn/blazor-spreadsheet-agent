using blazor_spreadsheet_agent.Data;
using blazor_spreadsheet_agent.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using System.Dynamic;

namespace blazor_spreadsheet_agent.Services;

public class QueryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QueryService> _logger;
    private readonly IConfiguration _configuration;

    public QueryService(
        ApplicationDbContext context, 
        ILogger<QueryService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<QueryResult> ExecuteQueryAsync(string sqlQuery, string? userId = null, string? clientIp = null)
    {
        var log = new QueryLog
        {
            Timestamp = DateTime.UtcNow,
            GeneratedSql = sqlQuery,
            UserId = userId,
            ClientIp = clientIp,
            WasSuccessful = false
        };

        try
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();
            
            using var command = connection.CreateCommand();
            command.CommandText = sqlQuery;
            command.CommandType = CommandType.Text;

            using var reader = await command.ExecuteReaderAsync();
            
            var results = new List<Dictionary<string, object>>();
            var columns = new List<string>();
            
            // Get column names
            for (var i = 0; i < reader.FieldCount; i++)
            {
                columns.Add(reader.GetName(i));
            }

            // Read data
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (var i = 0; i < columns.Count; i++)
                {
                    var columnName = columns[i];
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[columnName] = value ?? DBNull.Value;
                }
                results.Add(row);
            }

            log.WasSuccessful = true;
            log.RowsReturned = results.Count;
            
            return new QueryResult
            {
                Success = true,
                Data = results,
                Columns = columns,
                RowsAffected = results.Count
            };
        }
        catch (Exception ex)
        {
            log.Error = ex.Message;
            _logger.LogError(ex, "Error executing query: {Query}", sqlQuery);
            
            return new QueryResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            await _context.QueryLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<QueryLog>> GetQueryHistoryAsync(int limit = 50)
    {
        return await _context.QueryLogs
            .OrderByDescending(q => q.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
}

public class QueryResult
{
    public bool Success { get; set; }
    public List<Dictionary<string, object>>? Data { get; set; }
    public List<string>? Columns { get; set; }
    public int RowsAffected { get; set; }
    public string? ErrorMessage { get; set; }
}
