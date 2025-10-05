using Microsoft.EntityFrameworkCore;
using WondersAPI.Data;
using WondersAPI.Models;
using Serilog;
using Serilog.Formatting.Json; 

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = true;
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
});


var logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Logs", $"app-log{DateTime.Now:yyyyMMdd}.json");
Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);


Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(new JsonFormatter(), logFilePath, rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("WondersDb"));


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
            Console.WriteLine(" seed-data.json not found. No data was seeded.");
        }
    }
}

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation(" Wonders API application started successfully at {time}", DateTime.Now);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
