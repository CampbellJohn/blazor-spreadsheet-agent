# Blazor Spreadsheet Agent with AI Chat

A powerful Blazor Server application that combines spreadsheet management with an AI-powered chat interface. Built with .NET 8.0, this application allows users to interact with their data through natural language conversations.

## Features

- **AI-Powered Chat Interface**: Have natural conversations with an AI assistant
- **Spreadsheet Integration**: Connect chat to your spreadsheet data (coming soon)
- **Responsive Design**: Works on desktop and tablet devices
- **Modern UI**: Clean, intuitive interface with real-time updates
- **Secure**: Local processing of sensitive data with optional cloud AI integration

## Prerequisites

- .NET 8.0 SDK or later
- Node.js (for frontend dependencies)
- OpenAI API key (required for chat functionality)

## Getting Started

1. Clone the repository
2. Create an `appsettings.json` file in the project root with your OpenAI API key:
   ```json
   {
     "OpenAI": {
       "ApiKey": "your-openai-api-key-here"
     }
   }
   ```
3. Run the application:
   ```bash
   dotnet run
   ```
4. Open a browser and navigate to `https://localhost:5001/chat`

## Configuration

### OpenAI Integration
Add your OpenAI API key to `appsettings.json` to enable the chat functionality:

```json
{
  "OpenAI": {
    "ApiKey": "your-api-key-here"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

## Project Structure

- **/Models**: Data models for the application
  - `ChatModels.cs`: Data transfer objects for chat functionality
- **/Pages**: Blazor components
  - `Chat.razor`: Main chat interface
- **/Services**: Application services
  - `OpenAIService.cs`: Handles communication with OpenAI's API
- **/wwwroot**: Static files and client-side resources
  - `/js/chat.js`: Client-side JavaScript for chat functionality

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## Usage

1. Navigate to the chat interface at `/chat`
2. Type your message in the input box and press Enter or click Send
3. The AI assistant will respond to your queries
4. The chat maintains conversation history during your session

## Development

### Building the Project

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built with [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
- Powered by [OpenAI](https://openai.com/)
- Uses [Bootstrap](https://getbootstrap.com/) for styling