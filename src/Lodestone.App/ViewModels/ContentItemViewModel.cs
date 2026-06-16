using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lodestone.App.Services;
using Lodestone.Domain;
using Lodestone.Domain.Compatibility;

namespace Lodestone.App.ViewModels;

/// <summary>A single row in "My Content": the item plus its compatibility verdict shown as a symbol.</summary>
public sealed partial class ContentItemViewModel : ObservableObject
{
    private readonly InstalledContent _model;
    private readonly Func<string, Task> _onToggle;
    private readonly Func<string, Task> _onUninstall;

    public ContentItemViewModel(
        InstalledContent model,
        CompatibilityReport? report,
        bool showVersions,
        Func<string, Task> onToggle,
        Func<string, Task> onUninstall)
    {
        _model = model;
        _onToggle = onToggle;
        _onUninstall = onUninstall;
        Report = report;
        ShowVersions = showVersions;
        _enabled = model.Enabled;
    }

    public string Id => _model.Id;

    public string Name => _model.Name;

    public string AvatarLetter => char.ToUpperInvariant(Name.Length > 0 ? Name[0] : '?').ToString();

    public bool HasLoader => _model.Loader != Loader.None;

    public string LoaderLabel => _model.Loader.ToDisplayName();

    public string MetaLabel =>
        $"v{_model.Version}" + (HasLoader ? $"   ·   {LoaderLabel}" : string.Empty) + $"   ·   {Format.Size(_model.SizeMb)}";

    public bool UpdateAvailable => _model.UpdateAvailable;

    public bool ShowVersions { get; }

    public string VersionsLabel => "Supports " + string.Join(" · ", _model.GameVersions.Select(v => v.Value));

    [ObservableProperty]
    private bool _enabled;

    // ---- compatibility verdict ----
    public CompatibilityReport? Report { get; }

    public bool HasIssues => Report?.HasIssues == true;

    /// <summary>The mark drawn inside the severity badge ("!" for problems, "i" for info).</summary>
    public string IssueMark => Report?.HighestSeverity switch
    {
        CompatibilitySeverity.Error => "!",
        CompatibilitySeverity.Warning => "!",
        CompatibilitySeverity.Info => "i",
        _ => string.Empty,
    };

    public string IssueSeverity => Report?.HighestSeverity switch
    {
        CompatibilitySeverity.Error => "error",
        CompatibilitySeverity.Warning => "warning",
        CompatibilitySeverity.Info => "info",
        _ => "none",
    };

    public string IssueTooltip => Report is null || !Report.HasIssues
        ? string.Empty
        : string.Join(Environment.NewLine, Report.Issues.Select(i => "• " + i.Message));

    [RelayCommand]
    private Task ToggleAsync() => _onToggle(Id);

    [RelayCommand]
    private Task UninstallAsync() => _onUninstall(Id);
}
