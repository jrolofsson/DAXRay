using DAXRay.Core.Analysis;
using DAXRay.Core.Models;
using Xunit;

namespace DAXRay.Tests;

public class AnalyzerTests
{
    [Fact]
    public void Should_Find_Unused_Measures()
    {
        var measures = new List<MeasureDefinition>
        {
            new() { Table = "dummy", Name = "m1", Expression = "1" },
            new() { Table = "dummy", Name = "m2", Expression = "2" }
        };

        var usages = new List<ReportMeasureUsage>
        {
            new() { ReportName = "R1", MeasureRefs = new List<string> { "dummy.m1" } }
        };

        var analyzer = new Analyzer();
        var unused = analyzer.FindUnusedMeasures(measures, usages).ToList();

        Assert.Single(unused);
        Assert.Contains("dummy.m2", unused);
    }

    [Fact]
    public void Should_Find_Duplicate_Measures()
    {
        var measures = new List<MeasureDefinition>
        {
            new() { Table = "dummy", Name = "m1", Expression = "SUM(x)" },
            new() { Table = "dummy", Name = "m2", Expression = "SUM(x)" }
        };

        var analyzer = new Analyzer();
        var duplicates = analyzer.FindDuplicates(measures).ToList();

        Assert.Single(duplicates);
        Assert.Equal("dummy", duplicates[0].Table);
    }
}
