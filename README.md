# Blazor Spreadsheet Agent

A powerful Blazor Server application that allows users to upload, manage, and query spreadsheet data with natural language. Built with .NET 8.0, this application provides a user-friendly interface for working with spreadsheet data stored in a SQL database.

## Features

- **File Upload**: Upload XLSX, XLS, and CSV files with support for headers
- **Data Management**: View and manage uploaded spreadsheets in a clean, tabular interface
- **Natural Language Queries**: Use natural language to query your spreadsheet data (OpenAI integration required)
- **Batch Processing**: Efficiently process large files with batch processing
- **Responsive Design**: Works on desktop and tablet devices

## Prerequisites

- .NET 8.0 SDK or later
- SQL Server (LocalDB is sufficient for development)
- Node.js (for frontend dependencies)
- (Optional) OpenAI API key for natural language query features

## Getting Started

1. Clone the repository
2. Update the connection string in `appsettings.json` to point to your SQL Server instance
3. Run the following commands:
   ```
   dotnet restore
   dotnet ef database update
   dotnet run
   ```
4. Open a browser and navigate to `https://localhost:5001`

## Configuration

### Database
Update the connection string in `appsettings.json` with your SQL Server details:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BlazorSpreadsheetAgent;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

### OpenAI Integration (Optional)
To enable natural language queries, add your OpenAI API key to `appsettings.json`:

```json
"OpenAI": {
  "ApiKey": "your-api-key-here"
}
```

## Project Structure

- **/Data**: Database context and migrations
- **/Models**: Entity models (Spreadsheet, SpreadsheetData, QueryLog)
- **/Pages**: Blazor components for the UI
- **/Services**: Business logic and data access
- **/wwwroot**: Static files and client-side resources

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built with [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
- Uses [CsvHelper](https://joshclose.github.io/CsvHelper/) for CSV processing
- Uses [ExcelDataReader](https://github.com/ExcelData/ExcelDataReader) for Excel file processing