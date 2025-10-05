using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Json;
using WondersAPI.Data;
using WondersAPI.Models;
using System.Reflection; 

var builder = WebApplication.CreateBuilder(args);

// Logging setup
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});

// Serilog file path
var logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Logs", $"app-log{DateTime.Now:yyyyMMdd}.json");
Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(new JsonFormatter(), logFilePath, rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("WondersDb"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

//  Swagger + XML Comments
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Database seeding
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.Wonders.Any())
    {
        var jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "seed-data.json");
        if (File.Exists(jsonPath))
        {
            var json = File.ReadAllText(jsonPath);
            var wonders = System.Text.Json.JsonSerializer.Deserialize<List<Wonder>>(json) ?? new();
            db.Wonders.AddRange(wonders);
            db.SaveChanges();
        }
        else
        {
            Console.WriteLine("seed-data.json not found. No data was seeded.");
        }
    }
}

// Logging app start
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Wonders API application started successfully at {time}", DateTime.Now);

//  Swagger Middleware
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Wonders API V1");
    options.DocumentTitle = "Wonders API Docs";
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
