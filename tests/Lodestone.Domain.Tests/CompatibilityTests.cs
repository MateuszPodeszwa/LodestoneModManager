using Lodestone.Domain.Compatibility;

namespace Lodestone.Domain.Tests;

public class CompatibilityIssueTests
{
    [Fact]
    public void Error_factory_sets_severity_and_glyph()
    {
        var issue = CompatibilityIssue.Error(CompatibilityKind.MissingDependency, "Requires Fabric API", "Fabric API");

        issue.Severity.ShouldBe(CompatibilitySeverity.Error);
        issue.Glyph.ShouldBe("⛔");
        issue.RelatedName.ShouldBe("Fabric API");
    }

    [Fact]
    public void Warning_and_info_factories_set_their_severities()
    {
        CompatibilityIssue.Warning(CompatibilityKind.GameVersionMismatch, "x").Severity
            .ShouldBe(CompatibilitySeverity.Warning);
        CompatibilityIssue.Info(CompatibilityKind.OrphanLibrary, "x").Severity
            .ShouldBe(CompatibilitySeverity.Info);
    }
}

public class CompatibilityReportTests
{
    [Fact]
    public void Clean_report_has_no_issues_and_no_highest_severity()
    {
        var report = CompatibilityReport.Clean("sodium");

        report.HasIssues.ShouldBeFalse();
        report.HasErrors.ShouldBeFalse();
        report.HighestSeverity.ShouldBeNull();
    }

    [Fact]
    public void Highest_severity_is_the_worst_present()
    {
        var report = new CompatibilityReportBuilder("iris")
            .Add(CompatibilityIssue.Info(CompatibilityKind.OrphanLibrary, "info"))
            .Add(CompatibilityIssue.Error(CompatibilityKind.Conflict, "conflict"))
            .Add(CompatibilityIssue.Warning(CompatibilityKind.Duplicate, "dupe"))
            .Build();

        report.HasIssues.ShouldBeTrue();
        report.HasErrors.ShouldBeTrue();
        report.HighestSeverity.ShouldBe(CompatibilitySeverity.Error);
        report.Issues.Count.ShouldBe(3);
    }
}
