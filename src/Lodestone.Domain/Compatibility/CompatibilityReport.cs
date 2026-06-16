namespace Lodestone.Domain.Compatibility;

/// <summary>
/// The set of issues found for one piece of content. The UI binds <see cref="HighestSeverity"/> to
/// pick which symbol to show, and lists every issue in the tooltip.
/// </summary>
public sealed class CompatibilityReport
{
    public CompatibilityReport(string contentId, IReadOnlyList<CompatibilityIssue> issues)
    {
        ContentId = contentId;
        Issues = issues;
    }

    public string ContentId { get; }

    public IReadOnlyList<CompatibilityIssue> Issues { get; }

    public bool HasIssues => Issues.Count > 0;

    public bool HasErrors => Issues.Any(i => i.Severity == CompatibilitySeverity.Error);

    /// <summary>The worst severity present, or <c>null</c> when the item is clean.</summary>
    public CompatibilitySeverity? HighestSeverity
        => HasIssues ? Issues.Max(i => i.Severity) : null;

    public static CompatibilityReport Clean(string contentId) => new(contentId, []);
}

/// <summary>Incrementally assembles a <see cref="CompatibilityReport"/> as rules contribute issues.</summary>
public sealed class CompatibilityReportBuilder
{
    private readonly List<CompatibilityIssue> _issues = [];

    public CompatibilityReportBuilder(string contentId) => ContentId = contentId;

    public string ContentId { get; }

    public CompatibilityReportBuilder Add(CompatibilityIssue issue)
    {
        _issues.Add(issue);
        return this;
    }

    public CompatibilityReportBuilder AddRange(IEnumerable<CompatibilityIssue> issues)
    {
        _issues.AddRange(issues);
        return this;
    }

    public CompatibilityReport Build() => new(ContentId, _issues);
}
