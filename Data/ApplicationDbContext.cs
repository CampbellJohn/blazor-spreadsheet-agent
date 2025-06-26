using Microsoft.EntityFrameworkCore;
using blazor_spreadsheet_agent.Models;

namespace blazor_spreadsheet_agent.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Spreadsheet> Spreadsheets { get; set; } = null!;
    public DbSet<SpreadsheetData> SpreadsheetData { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Spreadsheet entity
        modelBuilder.Entity<Spreadsheet>()
            .HasMany(s => s.DataRows)
            .WithOne(d => d.Spreadsheet)
            .HasForeignKey(d => d.SpreadsheetId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Configure indexes for better query performance
        modelBuilder.Entity<SpreadsheetData>()
            .HasIndex(d => d.SpreadsheetId);

    }
}
