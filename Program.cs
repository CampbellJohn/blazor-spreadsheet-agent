using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using blazor_spreadsheet_agent.Data;
using blazor_spreadsheet_agent.Models;
using blazor_spreadsheet_agent.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add ApplicationDbContext with SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add services for dependency injection
builder.Services.AddScoped<SpreadsheetService>();
builder.Services.AddScoped<QueryService>();
builder.Services.AddScoped<FileProcessingService>();

// Register OpenAIService if API key is configured
var openAiApiKey = builder.Configuration["OpenAI:ApiKey"];
if (!string.IsNullOrEmpty(openAiApiKey))
{
    builder.Services.AddScoped<OpenAIService>();
}
else
{
    builder.Services.AddScoped<OpenAIService>(sp => 
        new OpenAIService(
            sp.GetRequiredService<ILogger<OpenAIService>>(),
            sp.GetRequiredService<IConfiguration>()));
}

// Add HTTP context accessor for services that need it
builder.Services.AddHttpContextAccessor();

// Add localization services
builder.Services.AddLocalization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
