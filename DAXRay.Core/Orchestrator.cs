using DAXRay.Core.Analysis;
using DAXRay.Core.Models;
using DAXRay.Core.Parsers;

namespace DAXRay.Core;

public class Orchestrator
{
    private readonly TmdlParser _tmdlParser = new();
    private readonly ReportParser _reportParser = new();
    private readonly Analyzer _analyzer = new();

    public IEnumerable<ModelAnalysisResult> AnalyzeAll(string rootDir)
    {
        var results = new List<ModelAnalysisResult>();

        // 1️⃣ discover models
        var modelDirs = Directory.GetDirectories(rootDir, "*.SemanticModel", SearchOption.AllDirectories);

        // 2️⃣ discover reports
        var reportDirs = Directory.GetDirectories(rootDir, "*.Report", SearchOption.AllDirectories);

        foreach (var modelDir in modelDirs)
        {
            var tablesDir = Path.Combine(modelDir, "definition", "tables");
            if (!Directory.Exists(tablesDir))
                continue;

            Console.WriteLine($"🔎 Analyzing model: {Path.GetFileName(modelDir)}");

            // parse measures
            var measures = _tmdlParser.ParseFromDirectory(tablesDir).ToList();

            // find reports bound to this model
            var reportsForModel = reportDirs.Where(r => MatchesModel(modelDir, r)).ToList();

            // parse report usages
            var usages = new List<ReportMeasureUsage>();
            foreach (var reportDir in reportsForModel)
            {
                usages.AddRange(_reportParser.ParseFromDirectory(reportDir));
            }

            // analysis
            var unused = _analyzer.FindUnusedMeasures(measures, usages).ToList();
            var duplicates = _analyzer.FindDuplicates(measures).ToList();

            results.Add(new ModelAnalysisResult
            {
                ModelName = Path.GetFileName(modelDir),
                Measures = measures,
                Usages = usages,
                UnusedMeasures = unused,
                DuplicateMeasures = duplicates
            });
        }

        return results;
    }

    private bool MatchesModel(string modelDir, string reportDir)
    {
        var modelName = Path.GetFileName(modelDir).Replace(".SemanticModel", "");
        var reportName = Path.GetFileName(reportDir).Replace(".Report", "");

        return reportName.Contains(modelName, StringComparison.OrdinalIgnoreCase);
    }
}
