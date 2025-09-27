using DAXRay.Core.Models;
using DAXRay.Core.Utils;
using Newtonsoft.Json.Linq;

namespace DAXRay.Core.Parsers
{

    public interface IReportParser
    {
        IEnumerable<ReportMeasureUsage> ParseFromDirectory(string directoryPath);
    }

    public class ReportParser : IReportParser
    {
        public IEnumerable<ReportMeasureUsage> ParseFromDirectory(string reportDir)
        {
            var results = new List<ReportMeasureUsage>();

            var reportJsonPath = Path.Combine(reportDir, "report.json");
            if (!File.Exists(reportJsonPath))
                return results;

            var json = File.ReadAllText(reportJsonPath);
            var root = JObject.Parse(json);

            // Extract report name from .platform if available
            var reportName = Path.GetFileName(reportDir);
            var platformPath = Path.Combine(reportDir, ".platform");
            if (File.Exists(platformPath))
            {
                try
                {
                    var platformJson = JObject.Parse(File.ReadAllText(platformPath));
                    var displayName = platformJson["metadata"]?["displayName"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(displayName))
                    {
                        reportName = displayName;
                    }
                }
                catch
                {
                    // fallback to folder name
                }
            }

            var usages = new ReportMeasureUsage
            {
                ReportName = reportName,
                MeasureRefs = new List<string>()
            };

            // Parse sections -> visualContainers -> config -> prototypeQuery
            var sections = root["sections"] as JArray;
            if (sections != null)
            {
                foreach (var section in sections)
                {
                    var visuals = section["visualContainers"] as JArray;
                    if (visuals == null) continue;

                    foreach (var visual in visuals)
                    {
                        var configStr = visual["config"]?.ToString();
                        if (string.IsNullOrWhiteSpace(configStr)) continue;

                        JObject? config;
                        try
                        {
                            config = JObject.Parse(configStr);
                        }
                        catch
                        {
                            continue;
                        }

                        var proto = config["singleVisual"]?["prototypeQuery"];
                        if (proto == null) continue;

                        // Build alias → table map
                        var aliasMap = new Dictionary<string, string>();
                        var fromArray = proto["From"] as JArray;
                        if (fromArray != null)
                        {
                            foreach (var f in fromArray)
                            {
                                var alias = f["Name"]?.ToString();
                                var entity = f["Entity"]?.ToString();
                                if (!string.IsNullOrEmpty(alias) && !string.IsNullOrEmpty(entity))
                                {
                                    aliasMap[alias] = entity;
                                }
                            }
                        }

                        // Extract measures
                        var selectArray = proto["Select"] as JArray;
                        if (selectArray != null)
                        {
                            foreach (var select in selectArray)
                            {
                                if (select["Measure"] != null)
                                {
                                    var source = select["Measure"]?["Expression"]?["SourceRef"]?["Source"]?.ToString();
                                    var property = select["Measure"]?["Property"]?.ToString();

                                    if (!string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(property))
                                    {
                                        if (aliasMap.TryGetValue(source, out var tableName))
                                            usages.MeasureRefs.Add($"{tableName}.{property}");
                                        else
                                            usages.MeasureRefs.Add($"{source}.{property}");
                                    }
                                    else
                                    {
                                        // fallback to "Name"
                                        var rawName = select["Name"]?.ToString();
                                        if (!string.IsNullOrWhiteSpace(rawName))
                                            usages.MeasureRefs.Add(rawName);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            usages.MeasureRefs = usages.MeasureRefs
                .Select(NameNormalizer.Normalize)
                .Distinct()
                .ToList();

            results.Add(usages);
            return results;
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

