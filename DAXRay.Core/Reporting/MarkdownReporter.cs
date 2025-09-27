namespace DAXRay.Core.Reporting;

using DAXRay.Core.Models;
using DAXRay.Core.Utils;
using System.Text;

public class MarkdownReporter
{
    public string RenderSummary(
        IEnumerable<MeasureDefinition> measures,
        IEnumerable<ReportMeasureUsage> usages,
        IEnumerable<string> unused,
        IEnumerable<DuplicateMeasureGroup> duplicates,
        UnknownReferencesResult? unknowns = null,
        Dictionary<string, HashSet<string>>? reportMeasureMap = null)
    {
        var sb = new StringBuilder();

        // Totals
        sb.AppendLine("### 📊 Totals");
        sb.AppendLine($"- **Total measures**: {measures.Count()}");
        sb.AppendLine($"- **Used measures**: {usages.SelectMany(u => u.MeasureRefs).Distinct().Count()}");
        sb.AppendLine($"- **Unused measures**: {unused.Count()}");
        sb.AppendLine($"- **Duplicate definitions**: {duplicates.Count()}");
        if (unknowns != null)
        {
            sb.AppendLine($"- **Unknown references**: {unknowns.BrokenMeasures.Count + unknowns.Columns.Count}");
        }
        sb.AppendLine();

        // Unused measures
        if (unused.Any())
        {
            sb.AppendLine("### 🗑️ Unused Measures");
            foreach (var u in unused.OrderBy(x => x))
            {
                sb.AppendLine($"- {u}");
            }
            sb.AppendLine();
        }

        // Duplicate measures
        if (duplicates.Any())
        {
            sb.AppendLine("### 🔁 Duplicate Measures");
            foreach (var group in duplicates)
            {
                sb.AppendLine($"#### Table: {group.Table}");
                sb.AppendLine();
                sb.AppendLine("```dax");
                sb.AppendLine(group.Expression);
                sb.AppendLine("```");
                sb.AppendLine("**Measures:**");
                foreach (var m in group.Measures)
                {
                    sb.AppendLine($"- {m}");
                }
                sb.AppendLine();
            }
        }

        // Heatmap
        if (reportMeasureMap != null && reportMeasureMap.Count != 0)
        {
            sb.AppendLine("### 📊 Measure Usage Heatmap");
            sb.AppendLine();

            var reports = reportMeasureMap.Keys.OrderBy(r => r).ToList();
            var normalizedReportMap = reportMeasureMap.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(NameNormalizer.Normalize).ToHashSet()
            );

            var allMeasures = measures
                .OrderBy(m => m.Table).ThenBy(m => m.Name)
                .ToList();

            // Header with usage count
            sb.AppendLine("| Measure | " + string.Join(" | ", reports) + " | Usage Count |");
            sb.AppendLine("|---------|" + string.Join("|", reports.Select(_ => "---------")) + "|-------------|");

            // Rows
            foreach (var measure in allMeasures)
            {
                var prettyName = $"{measure.Table}.{measure.Name}";
                var normalized = NameNormalizer.Normalize(prettyName);

                var row = new List<string> { prettyName };
                int count = 0;

                foreach (var report in reports)
                {
                    bool used = normalizedReportMap[report].Contains(normalized);
                    row.Add(used ? "✅" : "");
                    if (used) count++;
                }

                row.Add(count.ToString());

                sb.AppendLine("| " + string.Join(" | ", row) + " |");
            }

            sb.AppendLine();
        }

        // Unknown references
        if (unknowns != null && (unknowns.BrokenMeasures.Count != 0 || unknowns.Columns.Count != 0))
        {
            sb.AppendLine("### ❓ Unknown References");
            if (unknowns.BrokenMeasures.Count != 0)
            {
                sb.AppendLine("#### Measures not found");
                foreach (var bm in unknowns.BrokenMeasures.OrderBy(x => x))
                {
                    sb.AppendLine($"- {bm}");
                }
                sb.AppendLine();
            }
            if (unknowns.Columns.Count != 0)
            {
                sb.AppendLine("#### Column references");
                foreach (var col in unknowns.Columns.OrderBy(x => x))
                {
                    sb.AppendLine($"- {col}");
                }
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    public void WriteSummary(
        string path,
        IEnumerable<MeasureDefinition> measures,
        IEnumerable<ReportMeasureUsage> usages,
        IEnumerable<string> unused,
        IEnumerable<DuplicateMeasureGroup> duplicates,
        UnknownReferencesResult? unknowns = null,
        Dictionary<string, HashSet<string>>? reportMeasureMap = null)
    {
        var content = RenderSummary(measures, usages, unused, duplicates, unknowns, reportMeasureMap);
        File.WriteAllText(path, content);
    }
}
