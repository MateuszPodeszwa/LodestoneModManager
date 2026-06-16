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
        Func<CatalogItemViewModel, Task> install,
        Action<CatalogProject> open)
    {
        Project = project;
        _install = install;
        _open = open;
        _installed = installed;
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

    public bool CanInstall => !Installed && !Installing;

    partial void OnInstalledChanged(bool value) => OnPropertyChanged(nameof(CanInstall));

    partial void OnInstallingChanged(bool value) => OnPropertyChanged(nameof(CanInstall));

    [RelayCommand]
    private Task InstallAsync() => _install(this);

    [RelayCommand]
    private void Open() => _open(Project);
}
