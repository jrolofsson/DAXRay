namespace DAXRay.Core.Models
{
    public record ReportMeasureUsage
    {
        public string ReportName { get; init; } = string.Empty;
        public List<string> MeasureRefs { get; set; } = new();
    }
}

