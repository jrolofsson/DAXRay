namespace DAXRay.Core.Models
{
    public record MeasureDefinition
    {
        public string Table { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Expression { get; init; } = string.Empty;
        public string DisplayFolder { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
    }
}

