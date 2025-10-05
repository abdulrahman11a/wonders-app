using System.Text.Json;
using WondersAPI.Models;

namespace WondersAPI.Data
{
    public static class DataSeedingApplication
    {
        public static List<Wonder> LoadWondersFromJsonFile(string jsonFilePath) =>
            ReadAndConvertJsonToWonders(jsonFilePath);

        private static List<Wonder> ReadAndConvertJsonToWonders(string jsonFilePath)
        {
            ThrowIfJsonFileDoesNotExist(jsonFilePath);
            return ConvertJsonStringToWonderList(File.ReadAllText(jsonFilePath));
        }

        private static void ThrowIfJsonFileDoesNotExist(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
                throw new FileNotFoundException($"The seed data file was not found at path: {jsonFilePath}");
        }

        private static List<Wonder> ConvertJsonStringToWonderList(string jsonString) =>
            JsonSerializer.Deserialize<List<Wonder>>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Wonder>();
    }
}
