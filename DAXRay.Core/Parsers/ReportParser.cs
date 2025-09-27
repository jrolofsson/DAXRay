using DAXRay.Core.Models;
using Newtonsoft.Json.Linq;

namespace DAXRay.Core.Parsers
{

    public interface IReportParser
    {
        IEnumerable<ReportMeasureUsage> ParseFromDirectory(string directoryPath);
    }

    public class ReportParser : IReportParser
    {
        public IEnumerable<ReportMeasureUsage> ParseFromDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                yield break;

            foreach (var file in Directory.GetFiles(directoryPath, "*.json", SearchOption.AllDirectories))
            {
                var content = File.ReadAllText(file);
                var usages = new ReportMeasureUsage
                {
                    ReportName = GetReportName(file)
                };

                try
                {
                    var root = JObject.Parse(content);

                    // Traverse sections -> visualContainers -> config
                    var sectionTokens = root["sections"] ?? new JArray();
                    foreach (var section in sectionTokens)
                    {
                        var vcTokens = section["visualContainers"] ?? new JArray();
                        foreach (var vc in vcTokens)
                        {
                            var configToken = vc["config"];
                            if (configToken != null && configToken.Type == JTokenType.String)
                            {
                                try
                                {
                                    var configObj = JObject.Parse(configToken.ToString());
                                    ExtractMeasuresFromConfig(configObj, usages);
                                }
                                catch
                                {
                                    // skip malformed configs
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARN] Failed to parse {file}: {ex.Message}");
                }

                usages.MeasureRefs = usages.MeasureRefs.Distinct().ToList();
                yield return usages;
            }
        }

        private void ExtractMeasuresFromConfig(JObject config, ReportMeasureUsage usages)
        {
            var protoQuery = config["singleVisual"]?["prototypeQuery"];
            if (protoQuery == null) return;

            // Step 1: Build alias map
            var aliasMap = new Dictionary<string, string>();
            if (protoQuery["From"] is JArray froms)
            {
                foreach (var f in froms)
                {
                    var alias = f["Name"]?.ToString();
                    var entity = f["Entity"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(alias) && !string.IsNullOrWhiteSpace(entity))
                    {
                        aliasMap[alias] = entity;
                    }
                }
            }

            // Step 2: Extract measures
            if (protoQuery["Select"] is JArray selects)
            {
                foreach (var select in selects)
                {
                    var measureNode = select["Measure"];
                    if (measureNode != null)
                    {
                        var alias = measureNode["Expression"]?["SourceRef"]?["Source"]?.ToString();
                        var property = measureNode["Property"]?.ToString();

                        if (!string.IsNullOrWhiteSpace(alias) && !string.IsNullOrWhiteSpace(property))
                        {
                            var table = aliasMap.TryGetValue(alias, out var entity) ? entity : alias;
                            usages.MeasureRefs.Add($"{table}.{property}");
                        }
                    }
                }
            }
        }

        private string GetReportName(string reportJsonPath)
        {
            var dir = Path.GetDirectoryName(reportJsonPath);
            if (dir == null) return Path.GetFileNameWithoutExtension(reportJsonPath);

            var platformFile = Path.Combine(dir, ".platform");
            if (!File.Exists(platformFile))
                return Path.GetFileNameWithoutExtension(reportJsonPath);

            try
            {
                var content = File.ReadAllText(platformFile);
                var root = JObject.Parse(content);
                var name = root["metadata"]?["displayName"]?.ToString();
                if (!string.IsNullOrWhiteSpace(name))
                    return name;
            }
            catch
            {
                // ignore and fallback
            }

            return Path.GetFileNameWithoutExtension(reportJsonPath);
        }

    }

}

