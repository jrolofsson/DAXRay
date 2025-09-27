namespace DAXRay.Core.Reporting;

using DAXRay.Core.Models;
using System.Text;

public class MarkdownReporter
{
    public void WriteSummary(
        string path,
        IEnumerable<MeasureDefinition> measures,
        IEnumerable<ReportMeasureUsage> usages,
        IEnumerable<string> unused,
        IEnumerable<DuplicateMeasureGroup> duplicates,
        UnknownReferencesResult? unknowns = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# DAXRay Analysis Summary");
        sb.AppendLine();

        // Totals
        sb.AppendLine("## 📊 Totals");
        sb.AppendLine($"- **Total measures**: {measures.Count()}");
        sb.AppendLine($"- **Used measures**: {usages.SelectMany(u => u.MeasureRefs).Distinct().Count()}");
        sb.AppendLine($"- **Unused measures**: {unused.Count()}");
        sb.AppendLine($"- **Duplicate definitions**: {duplicates.Count()}");
        if (unknowns != null)
        {
            sb.AppendLine($"- **Unknown references**: {unknowns.BrokenMeasures.Count + unknowns.Columns.Count}");
        }
        sb.AppendLine();

        // Unused
        if (unused.Any())
        {
            sb.AppendLine("## 🗑️ Unused Measures");
            foreach (var u in unused.OrderBy(x => x))
            {
                sb.AppendLine($"- {u}");
            }
            sb.AppendLine();
        }

        // Duplicates
        if (duplicates.Any())
        {
            sb.AppendLine("## 🔁 Duplicate Measures");
            foreach (var group in duplicates)
            {
                sb.AppendLine($"### Table: {group.Table}");
                sb.AppendLine();
                sb.AppendLine("```dax");
                sb.AppendLine(group.Expression);
                sb.AppendLine("```");
                sb.AppendLine("**Measures:**");
                foreach (var m in group.Measures)
                {
                    sb.AppendLine($"- {m.Table}.{m.Name}");
                }
                sb.AppendLine();
            }
        }

        // Unknown references
        if (unknowns != null && (unknowns.BrokenMeasures.Count != 0 || unknowns.Columns.Count != 0))
        {
            sb.AppendLine("## ❓ Unknown References");
            if (unknowns.BrokenMeasures.Count != 0)
            {
                sb.AppendLine("### Measures not found");
                foreach (var bm in unknowns.BrokenMeasures.OrderBy(x => x))
                {
                    sb.AppendLine($"- {bm}");
                }
                sb.AppendLine();
            }
            if (unknowns.Columns.Count != 0)
            {
                sb.AppendLine("### Column references");
                foreach (var col in unknowns.Columns.OrderBy(x => x))
                {
                    sb.AppendLine($"- {col}");
                }
                sb.AppendLine();
            }
        }

        File.WriteAllText(path, sb.ToString());
    }
}
