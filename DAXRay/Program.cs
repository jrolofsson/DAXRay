using DAXRay.Core;
using DAXRay.Core.Parsers;
using DAXRay.Core.Analysis;
using DAXRay.Core.Reporting;

// ---------------------------------------------------
// Check if orchestrator mode is requested
// ---------------------------------------------------
if (args.Length > 0 && args[0].Equals("orchestrate", StringComparison.OrdinalIgnoreCase))
{
    var rootPath = args.ElementAtOrDefault(1) ?? ".";
    var outputPath = args.ElementAtOrDefault(2) ?? "./output";

    Directory.CreateDirectory(outputPath);

    var orchestrator = new Orchestrator();
    var results = orchestrator.AnalyzeAll(rootPath).ToList();

    // write per-model outputs
    var jsonReporter = new JsonReporter();
    foreach (var result in results)
    {
        var modelOut = Path.Combine(outputPath, result.ModelName);
        Directory.CreateDirectory(modelOut);

        jsonReporter.Write(result.Measures, Path.Combine(modelOut, "all_measures.json"));
        jsonReporter.Write(result.Usages, Path.Combine(modelOut, "measures_by_report.json"));
        jsonReporter.Write(result.UnusedMeasures, Path.Combine(modelOut, "unused_measures.json"));
        jsonReporter.Write(result.DuplicateMeasures, Path.Combine(modelOut, "duplicates.json"));
    }

    // consolidated summary with details
    var mdReporter = new MarkdownReporter();
    var summaryPath = Path.Combine(outputPath, "summary.md");

    using (var writer = new StreamWriter(summaryPath))
    {
        writer.WriteLine("# DAXRay Consolidated Analysis");
        writer.WriteLine();

        foreach (var result in results)
        {
            writer.WriteLine($"## 📂 Model: {result.ModelName}");
            writer.WriteLine();

            var perModelMd = mdReporter.RenderSummary(
                result.Measures,
                result.Usages,
                result.UnusedMeasures,
                result.DuplicateMeasures,
                null,                      
                result.ReportMeasureMap    
            );

            writer.Write(perModelMd);
        }
    }

    Console.WriteLine($"✅ Orchestration completed. Consolidated report at {summaryPath}");

    return;
}

// ---------------------------------------------------
// Default single-model analysis (v1 behavior)
// ---------------------------------------------------
var modelsPath = args.ElementAtOrDefault(Array.IndexOf(args, "--models") + 1) ?? ".";
var reportsPath = args.ElementAtOrDefault(Array.IndexOf(args, "--reports") + 1) ?? ".";
var outputPathSingle = args.ElementAtOrDefault(Array.IndexOf(args, "--output") + 1) ?? "./output";

Directory.CreateDirectory(outputPathSingle);

// Initialize parsers
var tmdlParser = new TmdlParser();
var reportParser = new ReportParser();

// Parse input
var measuresSingle = tmdlParser.ParseFromDirectory(modelsPath).ToList();
var usagesSingle = reportParser.ParseFromDirectory(reportsPath).ToList();

Console.WriteLine($"Parsed {measuresSingle.Count} measures from TMDL files.");
foreach (var m in measuresSingle.Take(5))
{
    Console.WriteLine($"- {m.Table}.{m.Name}");
}

// Analyze
var analyzerSingle = new Analyzer();
var unusedSingle = analyzerSingle.FindUnusedMeasures(measuresSingle, usagesSingle).ToList();
var duplicatesSingle = analyzerSingle.FindDuplicates(measuresSingle).ToList();

// Report
var jsonReporterSingle = new JsonReporter();
jsonReporterSingle.Write(measuresSingle, Path.Combine(outputPathSingle, "all_measures.json"));
jsonReporterSingle.Write(usagesSingle, Path.Combine(outputPathSingle, "measures_by_report.json"));
jsonReporterSingle.Write(unusedSingle, Path.Combine(outputPathSingle, "unused_measures.json"));
jsonReporterSingle.Write(duplicatesSingle, Path.Combine(outputPathSingle, "duplicates.json"));

var mdReporterSingle = new MarkdownReporter();
mdReporterSingle.WriteSummary(
    Path.Combine(outputPathSingle, "summary.md"),
    measuresSingle,
    usagesSingle,
    unusedSingle,
    duplicatesSingle
);

Console.WriteLine("✅ DAXRay single-model analysis completed.");

