using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lodestone.App.Services;
using Lodestone.Domain;

namespace Lodestone.App.ViewModels;

/// <summary>A card on the Browse screen.</summary>
public sealed partial class CatalogItemViewModel : ObservableObject
{
    private readonly Func<CatalogItemViewModel, Task> _install;
    private readonly Action<CatalogProject> _open;

    public CatalogItemViewModel(
        CatalogProject project,
        bool installed,
        bool compatible,
        string? incompatibilityReason,
        Func<CatalogItemViewModel, Task> install,
        Action<CatalogProject> open)
    {
        Project = project;
        _install = install;
        _open = open;
        _installed = installed;
        IsCompatible = compatible;
        IncompatibilityReason = incompatibilityReason ?? string.Empty;
        Chips = new ObservableCollection<string>(project.Categories.Take(2));
    }

    public CatalogProject Project { get; }

    public string Name => Project.Name;

    public string AvatarLetter => char.ToUpperInvariant(Name.Length > 0 ? Name[0] : '?').ToString();

    public string? IconUrl => Project.IconUrl;

    public bool HasIcon => !string.IsNullOrWhiteSpace(Project.IconUrl);

    public string AuthorLabel => "by " + Project.Author;

    public string Description => Project.Description;

    public string Downloads => Format.Number(Project.Downloads);

    public string Followers => Format.Number(Project.Followers);

    public ObservableCollection<string> Chips { get; }

    [ObservableProperty]
    private bool _installed;

    [ObservableProperty]
    private bool _installing;

    /// <summary>Whether this project has a build for the active loader + version, decided from the
    /// catalog metadata at search time (see <see cref="CatalogCompatibility"/>).</summary>
    public bool IsCompatible { get; }

    /// <summary>Show the "Incompatible" badge in place of the Install button: the project doesn't
    /// support the active version and isn't already installed (an installed copy shows "Installed").</summary>
    public bool IsIncompatible => !IsCompatible && !Installed;

    /// <summary>Tooltip for the incompatible badge - why it can't be installed for the active version.</summary>
    public string IncompatibilityReason { get; }

    public bool CanInstall => !Installed && !Installing && IsCompatible;

    partial void OnInstalledChanged(bool value)
    {
        OnPropertyChanged(nameof(CanInstall));
        OnPropertyChanged(nameof(IsIncompatible));
    }

    partial void OnInstallingChanged(bool value) => OnPropertyChanged(nameof(CanInstall));

    [RelayCommand]
    private Task InstallAsync() => _install(this);

    [RelayCommand]
    private void Open() => _open(Project);
}
