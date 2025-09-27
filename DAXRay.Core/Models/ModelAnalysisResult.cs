using DAXRay.Core.Models;

namespace DAXRay.Core;

public class ModelAnalysisResult
{
    public string ModelName { get; set; } = string.Empty;
    public List<MeasureDefinition> Measures { get; set; } = new();
    public List<ReportMeasureUsage> Usages { get; set; } = new();
    public List<string> UnusedMeasures { get; set; } = new();
    public List<DuplicateMeasureGroup> DuplicateMeasures { get; set; } = new();
    public Dictionary<string, HashSet<string>> ReportMeasureMap { get; set; } = new();
}
