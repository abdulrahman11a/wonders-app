using System.Text.Json;
using WondersAPI.Models;

namespace WondersAPI.Data
{
    public static class DataSeedingApplication
    {
        public static List<Wonder> SeedWonders(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Seed data file not found: {filePath}");

            var json = File.ReadAllText(filePath);
            var wonders = JsonSerializer.Deserialize<List<Wonder>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return wonders ?? new List<Wonder>();
        }
    }
}