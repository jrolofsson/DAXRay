namespace DAXRay.Core.Models
{
    public record DuplicateMeasure
    {
        public string Table { get; init; } = string.Empty;
        public string NormalizedExpression { get; init; } = string.Empty;
        public List<string> Duplicates { get; init; } = new();
    }

    public class DuplicateMeasureGroup
    {
        public string Table { get; set; } = string.Empty;
        public string Expression { get; set; } = string.Empty;
        public List<MeasureDefinition> Measures { get; set; } = new();
    }

    public record UnknownReferencesResult
    {
        public List<string> Columns { get; init; } = new();
        public List<string> BrokenMeasures { get; init; } = new();
    }
}

