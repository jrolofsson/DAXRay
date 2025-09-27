using DAXRay.Core.Analysis;
using DAXRay.Core.Models;
using DAXRay.Core.Parsers;
using DAXRay.Core.Utils;

namespace DAXRay.Core;

public class Orchestrator
{
    private readonly TmdlParser _tmdlParser = new();
    private readonly ReportParser _reportParser = new();
    private readonly Analyzer _analyzer = new();

    public IEnumerable<ModelAnalysisResult> AnalyzeAll(string rootDir)
    {
        var results = new List<ModelAnalysisResult>();

        // discover models
        var modelDirs = Directory.GetDirectories(rootDir, "*.SemanticModel", SearchOption.AllDirectories);

        // discover reports
        var reportDirs = Directory.GetDirectories(rootDir, "*.Report", SearchOption.AllDirectories);

        // collect logicalIds for models + reports
        var modelInfos = modelDirs.Select(d => new
        {
            Path = d,
            LogicalId = PlatformParser.GetLogicalId(d)
        }).ToList();

        var reportInfos = reportDirs.Select(d => new
        {
            Path = d,
            LogicalId = PlatformParser.GetLogicalId(d)
        }).ToList();

        foreach (var model in modelInfos)
        {
            var tablesDir = Path.Combine(model.Path, "definition", "tables");
            if (!Directory.Exists(tablesDir))
                continue;

            Console.WriteLine($"🔎 Analyzing model: {Path.GetFileName(model.Path)} (logicalId={model.LogicalId})");

            // parse measures
            var measures = _tmdlParser.ParseFromDirectory(tablesDir).ToList();

            // find reports bound to this model (by logicalId)
            var reportsForModel = reportInfos
                .Where(r =>
                {
                    var boundModelPath = ReportBindingResolver.GetBoundModelPath(r.Path);
                    if (boundModelPath == null) return false;

                    // normalize paths
                    var modelFullPath = Path.GetFullPath(model.Path);
                    return string.Equals(boundModelPath, modelFullPath, StringComparison.OrdinalIgnoreCase);
                })
                .ToList();

            Console.WriteLine($"   Matched reports: {(reportsForModel.Count != 0 ? string.Join(", ", reportsForModel.Select(r => Path.GetFileName(r.Path))) : "none")}");

            // parse report usages
            var usages = new List<ReportMeasureUsage>();
            foreach (var report in reportsForModel)
            {
                usages.AddRange(_reportParser.ParseFromDirectory(report.Path));
            }

            // analysis
            var unused = _analyzer.FindUnusedMeasures(measures, usages).ToList();
            var duplicates = _analyzer.FindDuplicates(measures).ToList();

            var reportMeasureMap = new Dictionary<string, HashSet<string>>();
            foreach (var usage in usages)
            {
                if (!reportMeasureMap.ContainsKey(usage.ReportName))
                    reportMeasureMap[usage.ReportName] = new HashSet<string>();

                foreach (var m in usage.MeasureRefs)
                    reportMeasureMap[usage.ReportName].Add(m);
            }

            foreach (var kvp in reportMeasureMap)
            {
                Console.WriteLine($"Report {kvp.Key} has {kvp.Value.Count} measure refs:");
                foreach (var m in kvp.Value.Take(5))
                {
                    Console.WriteLine($"   {m}");
                }
            }



            results.Add(new ModelAnalysisResult
            {
                ModelName = Path.GetFileName(model.Path),
                Measures = measures,
                Usages = usages,
                UnusedMeasures = unused,
                DuplicateMeasures = duplicates,
                ReportMeasureMap = reportMeasureMap
            });
        }

        return results;
    }
}
