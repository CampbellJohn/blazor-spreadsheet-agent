using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace blazor_spreadsheet_agent.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QueryLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NaturalLanguageQuery = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    GeneratedSql = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Error = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    WasSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    RowsReturned = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ClientIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueryLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Spreadsheets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Spreadsheets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpreadsheetData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SpreadsheetId = table.Column<int>(type: "int", nullable: false),
                    SheetName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    JsonData = table.Column<string>(type: "nvarchar(MAX)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpreadsheetData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpreadsheetData_Spreadsheets_SpreadsheetId",
                        column: x => x.SpreadsheetId,
                        principalTable: "Spreadsheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QueryLogs_Timestamp",
                table: "QueryLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_QueryLogs_UserId",
                table: "QueryLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SpreadsheetData_SpreadsheetId",
                table: "SpreadsheetData",
                column: "SpreadsheetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QueryLogs");

            migrationBuilder.DropTable(
                name: "SpreadsheetData");

            migrationBuilder.DropTable(
                name: "Spreadsheets");
        }
    }
}
