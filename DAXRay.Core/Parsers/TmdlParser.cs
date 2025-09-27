using System.Text.RegularExpressions;
using DAXRay.Core.Models;

namespace DAXRay.Core.Parsers
{
    public interface ITmdlParser
    {
        IEnumerable<MeasureDefinition> ParseFromDirectory(string directoryPath);
    }

    public class TmdlParser : ITmdlParser
    {
        public IEnumerable<MeasureDefinition> ParseFromDirectory(string directoryPath)
        {
            var measures = new List<MeasureDefinition>();

            foreach (var file in Directory.GetFiles(directoryPath, "*.tmdl", SearchOption.AllDirectories))
            {
                var content = File.ReadAllLines(file);
                string? currentTable = null;

                for (int i = 0; i < content.Length; i++)
                {
                    var line = content[i].Trim();

                    // detect table
                    if (line.StartsWith("table ", StringComparison.OrdinalIgnoreCase))
                    {
                        currentTable = line.Split(' ', 2)[1].Trim();
                        continue;
                    }

                    // detect measure
                    if (line.StartsWith("measure ", StringComparison.OrdinalIgnoreCase))
                    {
                        // 1️⃣ Extract measure name
                        var match = Regex.Match(line, @"measure\s+(?:'([^']+)'|(\S+))\s*=");
                        if (!match.Success) continue;

                        var name = !string.IsNullOrEmpty(match.Groups[1].Value)
                            ? match.Groups[1].Value
                            : match.Groups[2].Value;

                        string expression = "";

                        // 2️⃣ Extract expression (backtick block, inline, or multiline)
                        var afterEquals = line.Substring(match.Length).Trim();

                        if (afterEquals.StartsWith("```")) // backtick block
                        {
                            var exprLines = new List<string>();
                            // consume until closing ```
                            i++;
                            while (i < content.Length && !content[i].Trim().StartsWith("```"))
                            {
                                exprLines.Add(content[i]);
                                i++;
                            }
                            expression = string.Join(Environment.NewLine, exprLines);
                        }
                        else if (!string.IsNullOrEmpty(afterEquals)) // inline
                        {
                            expression = afterEquals;
                        }
                        else // multiline without backticks
                        {
                            var exprLines = new List<string>();
                            i++;
                            while (i < content.Length &&
                                   !content[i].TrimStart().StartsWith("lineageTag", StringComparison.OrdinalIgnoreCase) &&
                                   !content[i].TrimStart().StartsWith("changedProperty", StringComparison.OrdinalIgnoreCase) &&
                                   !content[i].TrimStart().StartsWith("annotation", StringComparison.OrdinalIgnoreCase) &&
                                   !content[i].TrimStart().StartsWith("measure", StringComparison.OrdinalIgnoreCase) &&
                                   !content[i].TrimStart().StartsWith("table", StringComparison.OrdinalIgnoreCase))
                            {
                                exprLines.Add(content[i]);
                                i++;
                            }
                            i--; // step back one line so outer loop reprocesses tag/next measure
                            expression = string.Join(Environment.NewLine, exprLines);
                        }

                        measures.Add(new MeasureDefinition
                        {
                            Table = currentTable ?? "Unknown",
                            Name = name.Trim(),
                            Expression = expression.Trim()
                        });
                    }
                }
            }

            return measures;
        }
    }
}

