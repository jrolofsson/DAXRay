namespace DAXRay.Core.Analysis;

using DAXRay.Core.Models;
using DAXRay.Core.Utils;

public class Analyzer
{
    public IEnumerable<string> FindUnusedMeasures(
        IEnumerable<MeasureDefinition> measures,
        IEnumerable<ReportMeasureUsage> reportUsages)
    {
        var allMeasures = measures
            .Select(m => NameNormalizer.Normalize($"{m.Table}.{m.Name}"))
            .ToHashSet();

        var normalizedUsed = reportUsages
            .SelectMany(r => r.MeasureRefs)
            .Select(NameNormalizer.Normalize)
            .Where(allMeasures.Contains)   // ✅ keep only true measures
            .ToHashSet();

        return measures
            .Where(m => !normalizedUsed.Contains(NameNormalizer.Normalize($"{m.Table}.{m.Name}")))
            .Select(m => $"{m.Table}.{m.Name}");
    }

    public IEnumerable<DuplicateMeasureGroup> FindDuplicates(IEnumerable<MeasureDefinition> measures)
    {
        return measures
            .GroupBy(m => m.Table)
            .SelectMany(g =>
                g.GroupBy(m => NormalizeExpression(m.Expression))
                 .Where(gg => gg.Count() > 1)
                 .Select(gg => new DuplicateMeasureGroup
                 {
                     Table = g.Key,
                     Expression = gg.Key,
                     Measures = gg.ToList()
                 }));
    }

    public int CountUsedMeasures(
        IEnumerable<MeasureDefinition> measures,
        IEnumerable<ReportMeasureUsage> reportUsages)
    {
        var allMeasures = measures
            .Select(m => NameNormalizer.Normalize($"{m.Table}.{m.Name}"))
            .ToHashSet();

        return reportUsages
            .SelectMany(r => r.MeasureRefs)
            .Select(NameNormalizer.Normalize)
            .Where(allMeasures.Contains)   // ✅ only real measures
            .Distinct()
            .Count();
    }

    private string NormalizeExpression(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return string.Empty;

        return expression
            .Replace(" ", string.Empty)
            .Replace("\t", string.Empty)
            .Replace("\n", string.Empty)
            .Replace("\r", string.Empty)
            .ToLowerInvariant();
    }

    public UnknownReferencesResult FindUnknownReferences(
        IEnumerable<MeasureDefinition> measures,
        IEnumerable<ReportMeasureUsage> reportUsages)
    {
        var allMeasures = measures
            .Select(m => NameNormalizer.Normalize($"{m.Table}.{m.Name}"))
            .ToHashSet();

        var unknowns = reportUsages
            .SelectMany(r => r.MeasureRefs)
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Where(r => !allMeasures.Contains(NameNormalizer.Normalize(r)))
            .Distinct();

        var result = new UnknownReferencesResult();

        foreach (var u in unknowns)
        {
            // Heuristic: if it's "table.column" → treat as column
            if (u.Contains('.') && char.IsLower(u.Split('.').LastOrDefault()?.FirstOrDefault() ?? 'a'))
            {
                result.Columns.Add(u);
            }
            else
            {
                result.BrokenMeasures.Add(u);
            }
        }

        return result;
    }
}
