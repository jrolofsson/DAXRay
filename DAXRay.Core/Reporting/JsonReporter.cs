namespace DAXRay.Core.Reporting;

using System.Text.Json;

public class JsonReporter
{
    public void Write<T>(IEnumerable<T> data, string filePath)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }
}
