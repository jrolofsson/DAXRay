using Newtonsoft.Json.Linq;

namespace DAXRay.Core.Utils
{
    public static class ReportBindingResolver
    {
        public static string? GetBoundModelPath(string reportDir)
        {
            var defFile = Path.Combine(reportDir, "definition.pbir");
            if (!File.Exists(defFile))
                return null;

            var json = File.ReadAllText(defFile);
            var obj = JObject.Parse(json);
            var relPath = obj["datasetReference"]?["byPath"]?["path"]?.ToString();
            if (string.IsNullOrWhiteSpace(relPath))
                return null;

            return Path.GetFullPath(Path.Combine(reportDir, relPath));
        }
    }

}

