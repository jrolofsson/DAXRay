using System.Collections.Generic;
using System.IO;
using Xunit;
using DAXRay.Core.Reporting;
using DAXRay.Core.Models;

namespace DAXRay.Tests
    {
        public class MarkdownReporterGoldenTests
        {
            [Fact]
            public void RenderSummary_ShouldMatchGoldenFile_WithHeatmap()
            {
                // Arrange
                var reporter = new MarkdownReporter();

                var measures = new List<MeasureDefinition>
            {
                new MeasureDefinition { Table = "Sales", Name = "TotalRevenue", Expression = "SUM(Sales[Revenue])" },
                new MeasureDefinition { Table = "Sales", Name = "NetRevenue", Expression = "SUM(Sales[Revenue])-SUM(Sales[Cost])" }
            };

                var usages = new List<ReportMeasureUsage>
            {
                new ReportMeasureUsage
                {
                    ReportName = "ReportA",
                    MeasureRefs = new List<string> { "Sales.TotalRevenue" }
                },
                new ReportMeasureUsage
                {
                    ReportName = "ReportB",
                    MeasureRefs = new List<string>() // No refs in ReportB
                }
            };

                var unused = new List<string> { "Sales.NetRevenue" };

                var duplicates = new List<DuplicateMeasureGroup>
            {
                new DuplicateMeasureGroup
                {
                    Table = "Sales",
                    Expression = "SUM(Sales[Revenue])-SUM(Sales[Cost])",
                    Measures = new List<MeasureDefinition>
                    {
                        new MeasureDefinition { Table = "Sales", Name = "NetRevenue" },
                        new MeasureDefinition { Table = "Sales", Name = "Profit" }
                    }
                }
            };

                var reportMeasureMap = new Dictionary<string, HashSet<string>>
            {
                { "ReportA", new HashSet<string>{ "Sales.TotalRevenue" } },
                { "ReportB", new HashSet<string>() }
            };

                // Act
                var markdown = reporter.RenderSummary(measures, usages, unused, duplicates, null, reportMeasureMap);

                // Assert
                var goldenPath = Path.Combine("Resources", "MarkdownReporter.md");
                var expected = File.ReadAllText(goldenPath).Trim();

                Assert.Equal(expected, markdown.Trim());
            }
        }
    }

