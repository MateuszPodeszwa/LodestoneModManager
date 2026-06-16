using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lodestone.App.Services;
using Lodestone.Application.Abstractions;
using Lodestone.Application.Settings;
using Lodestone.Domain;
using Lodestone.Domain.Common;

namespace Lodestone.App.ViewModels;

/// <summary>The first-run onboarding flow (welcome → detect game → preferences → done).</summary>
public sealed partial class OnboardingViewModel : ObservableObject
{
    private readonly ISettingsStore _settings;
    private readonly IGameLocator _locator;
    private readonly IDialogService _dialog;

    public OnboardingViewModel(ISettingsStore settings, IGameLocator locator, IDialogService dialog)
    {
        _settings = settings;
        _locator = locator;
        _dialog = dialog;

        Result<string> detected = _locator.Detect();
        _isGameDetected = detected.IsSuccess;
        _gameDir = detected.IsSuccess ? detected.Value : "Not found — choose your .minecraft folder";
    }

    /// <summary>Raised when onboarding finishes so the shell can dismiss the overlay.</summary>
    public event Action? Completed;

    [ObservableProperty] private int _step;
    [ObservableProperty] private string _obLoader = "fabric";
    [ObservableProperty] private bool _obAuto = true;
    [ObservableProperty] private string _gameDir;
    [ObservableProperty] private bool _isGameDetected;

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
    private void Locate()
    {
        string? picked = _dialog.PickFolder(_isGameDetected ? GameDir : null);
        if (picked is null)
        {
            return;
        }

        if (_locator.IsValid(picked))
        {
            GameDir = picked;
            IsGameDetected = true;
        }
        else
        {
            GameDir = "That folder doesn't look like a Minecraft install — try again.";
            IsGameDetected = false;
        }
    }

    [RelayCommand]
    private Task NextAsync()
    {
        if (Step < 3)
        {
            Step++;
            return Task.CompletedTask;
        }

        return FinishAsync();
    }

    [RelayCommand]
    private Task SkipAsync() => FinishAsync();

    private async Task FinishAsync()
    {
        LodestoneSettings s = _settings.Current.Clone();
        s.DefaultLoader = ObLoader.ParseLoader();
        s.AutoUpdate = ObAuto;
        s.OnboardingCompleted = true;
        if (IsGameDetected && _locator.IsValid(GameDir))
        {
            s.GameDirectory = GameDir;
        }

        // Awaited so the "onboarding completed" flag is durably written before we dismiss the overlay —
        // this is what guarantees onboarding only ever runs once.
        await _settings.SaveAsync(s).ConfigureAwait(true);
        Completed?.Invoke();
    }
}
