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
    private readonly Action _openExternal;

    public DetailViewModel(
        CatalogProject project,
        bool installed,
        bool compatible,
        string? incompatibilityReason,
        Func<DetailViewModel, Task> install,
        Action close,
        Action openExternal)
    {
        _project = project;
        _install = install;
        _close = close;
        _openExternal = openExternal;
        _installed = installed;
        IsCompatible = compatible;
        IncompatibilityReason = incompatibilityReason ?? string.Empty;
        Chips = new ObservableCollection<string>(project.Categories);
    }

    public string OpenLabel => _project.Source == "curseforge" ? "Open on CurseForge" : "Open on Modrinth";

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

    public string Description => string.IsNullOrWhiteSpace(_project.Body) ? _project.Description : _project.Body!;

    public string? IconUrl => _project.IconUrl;

    public bool HasIcon => !string.IsNullOrWhiteSpace(_project.IconUrl);

    public IReadOnlyList<string> GalleryUrls => _project.GalleryUrls ?? [];

    public bool HasGallery => GalleryUrls.Count > 0;

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

    /// <summary>Whether this project has a build for the active loader + version (see
    /// <see cref="CatalogCompatibility"/>); when false the modal blocks install and explains why.</summary>
    public bool IsCompatible { get; }

    /// <summary>Show the incompatible notice in place of the install button: unsupported and not
    /// already installed.</summary>
    public bool IsIncompatible => !IsCompatible && !Installed;

    /// <summary>The reason this project can't be installed for the active version.</summary>
    public string IncompatibilityReason { get; }

    public bool CanInstall => !Installed && !Installing && IsCompatible;

    public string InstallPercentLabel => $"{InstallPercent}%";

    partial void OnInstalledChanged(bool value)
    {
        OnPropertyChanged(nameof(CanInstall));
        OnPropertyChanged(nameof(IsIncompatible));
    }

    partial void OnInstallingChanged(bool value) => OnPropertyChanged(nameof(CanInstall));

    partial void OnInstallPercentChanged(int value) => OnPropertyChanged(nameof(InstallPercentLabel));

    [RelayCommand]
    private Task InstallAsync() => _install(this);

    [RelayCommand]
    private void Close() => _close();

    [RelayCommand]
    private void OpenExternal() => _openExternal();
}
