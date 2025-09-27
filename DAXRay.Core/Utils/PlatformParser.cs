using Newtonsoft.Json.Linq;

namespace DAXRay.Core.Utils
{
    public static class PlatformParser
    {
        public static string? GetLogicalId(string platformFilePath)
        {
            if (!File.Exists(platformFilePath))
                return null;

            var json = File.ReadAllText(platformFilePath);
            var obj = JObject.Parse(json);
            return obj["config"]?["logicalId"]?.ToString();
        }
    }
}

