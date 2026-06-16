using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lodestone.App.Services;
using Lodestone.Domain;

namespace Lodestone.App.ViewModels;

/// <summary>The detail modal shown when a Browse card is opened.</summary>
public sealed partial class DetailViewModel : ObservableObject
{
    private readonly CatalogProject _project;
    private readonly Func<DetailViewModel, Task> _install;
    private readonly Action _close;

    public DetailViewModel(
        CatalogProject project,
        bool installed,
        Func<DetailViewModel, Task> install,
        Action close)
    {
        _project = project;
        _install = install;
        _close = close;
        _installed = installed;
        Chips = new ObservableCollection<string>(project.Categories);
    }

    public CatalogProject Project => _project;

    public string Name => _project.Name;

    public string AvatarLetter => char.ToUpperInvariant(Name.Length > 0 ? Name[0] : '?').ToString();

    public string SubtitleLabel => $"by {_project.Author}  ·  {_project.Type.ToDisplayName()}  ·  {SourceLabel}";

    public string SourceLabel => _project.Source switch
    {
        "modrinth" => "Modrinth",
        "curseforge" => "CurseForge",
        _ => _project.Source,
    };

    public string Downloads => Format.Number(_project.Downloads);

    public string Followers => Format.Number(_project.Followers);

    public string Latest => string.IsNullOrWhiteSpace(_project.LatestVersion) ? "—" : _project.LatestVersion!;

    public string Description => _project.Description;

    public ObservableCollection<string> Chips { get; }

    public string VersionsLabel => string.Join("  ·  ", _project.GameVersions.Select(v => v.Value));

    public string LoadersLabel => _project.Loaders.Count > 0
        ? string.Join(", ", _project.Loaders.Select(l => l.ToDisplayName()))
        : "Pack — no loader needed";

    [ObservableProperty]
    private bool _installed;

    [ObservableProperty]
    private bool _installing;

    [ObservableProperty]
    private int _installPercent;

    public bool CanInstall => !Installed && !Installing;

    public string InstallPercentLabel => $"{InstallPercent}%";

    partial void OnInstalledChanged(bool value) => OnPropertyChanged(nameof(CanInstall));

    partial void OnInstallingChanged(bool value) => OnPropertyChanged(nameof(CanInstall));

    partial void OnInstallPercentChanged(int value) => OnPropertyChanged(nameof(InstallPercentLabel));

    [RelayCommand]
    private Task InstallAsync() => _install(this);

    [RelayCommand]
    private void Close() => _close();
}
