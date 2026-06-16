using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lodestone.Application.Abstractions;
using Lodestone.Application.Settings;
using Lodestone.Domain;

namespace Lodestone.App.ViewModels;

/// <summary>The first-run onboarding flow (welcome → detect game → preferences → done).</summary>
public sealed partial class OnboardingViewModel : ObservableObject
{
    private readonly ISettingsStore _settings;
    private readonly IGameLocator _locator;

    public OnboardingViewModel(ISettingsStore settings, IGameLocator locator)
    {
        _settings = settings;
        _locator = locator;
        _gameDir = _locator.Detect().Match(path => path, _ => "Not found — you can set it in Settings");
    }

    /// <summary>Raised when onboarding finishes so the shell can dismiss the overlay.</summary>
    public event Action? Completed;

    [ObservableProperty] private int _step;
    [ObservableProperty] private string _obLoader = "fabric";
    [ObservableProperty] private bool _obAuto = true;
    [ObservableProperty] private string _gameDir;

    public bool IsStep0 => Step == 0;
    public bool IsStep1 => Step == 1;
    public bool IsStep2 => Step == 2;
    public bool IsStep3 => Step == 3;
    public bool NotLast => Step < 3;
    public string NextLabel => Step switch { 0 => "Get started", 3 => "Finish setup", _ => "Continue" };
    public bool Dot0 => Step >= 0;
    public bool Dot1 => Step >= 1;
    public bool Dot2 => Step >= 2;
    public bool Dot3 => Step >= 3;

    partial void OnStepChanged(int value)
    {
        OnPropertyChanged(nameof(IsStep0));
        OnPropertyChanged(nameof(IsStep1));
        OnPropertyChanged(nameof(IsStep2));
        OnPropertyChanged(nameof(IsStep3));
        OnPropertyChanged(nameof(NotLast));
        OnPropertyChanged(nameof(NextLabel));
        OnPropertyChanged(nameof(Dot0));
        OnPropertyChanged(nameof(Dot1));
        OnPropertyChanged(nameof(Dot2));
        OnPropertyChanged(nameof(Dot3));
    }

    [RelayCommand]
    private void Next()
    {
        if (Step < 3)
        {
            Step++;
        }
        else
        {
            Finish();
        }
    }

    [RelayCommand]
    private void Skip() => Finish();

    private void Finish()
    {
        LodestoneSettings s = _settings.Current.Clone();
        s.DefaultLoader = ObLoader.ParseLoader();
        s.AutoUpdate = ObAuto;
        s.OnboardingCompleted = true;
        if (_locator.IsValid(GameDir))
        {
            s.GameDirectory = GameDir;
        }

        _ = _settings.SaveAsync(s);
        Completed?.Invoke();
    }
}
