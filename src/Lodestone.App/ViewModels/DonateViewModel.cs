using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lodestone.App.Services;
using Lodestone.Application.Messaging;
using Lodestone.Application.Supporter;
using Lodestone.Domain.Common;

namespace Lodestone.App.ViewModels;

public sealed class DonateTierViewModel
{
    public DonateTierViewModel(string name, string tag, int amount, bool featured, string[] perks)
    {
        Name = name;
        Tag = tag;
        Amount = amount;
        Featured = featured;
        Perks = new ObservableCollection<string>(perks);
    }

    public string Name { get; }
    public string Tag { get; }
    public int Amount { get; }
    public bool Featured { get; }
    public ObservableCollection<string> Perks { get; }
    public string AmountLabel { get; private set; } = string.Empty;
    public string ButtonLabel { get; private set; } = string.Empty;

    public void SetCycle(bool monthly)
    {
        AmountLabel = monthly ? $"${Amount}/mo" : $"${Amount}";
        ButtonLabel = $"Donate {AmountLabel}";
    }
}

/// <summary>The Support screen — Patreon links plus offline supporter-code redemption (cosmetic perks).</summary>
public sealed partial class DonateViewModel : ObservableObject
{
    // Replace with your real Patreon page; see docs/SUPPORTERS.md.
    private const string PatreonUrl = "https://www.patreon.com/lodestone";

    private readonly SupporterService _supporter;
    private readonly IDialogService _dialog;
    private readonly IMessageBus _bus;

    public DonateViewModel(SupporterService supporter, IDialogService dialog, IMessageBus bus)
    {
        _supporter = supporter;
        _dialog = dialog;
        _bus = bus;

        Tiers =
        [
            new DonateTierViewModel("Coffee", "A small thanks", 3, false,
                ["Supporter badge in the app", "Our sincere gratitude"]),
            new DonateTierViewModel("Supporter", "Most popular", 7, true,
                ["Everything in Coffee", "Early access to beta builds", "Extra accent themes"]),
            new DonateTierViewModel("Champion", "Power the project", 15, false,
                ["Everything in Supporter", "Vote on the roadmap", "Priority support"]),
        ];

        ApplyCycle();
    }

    public ObservableCollection<DonateTierViewModel> Tiers { get; }

    [ObservableProperty] private string _donateCycle = "once";
    [ObservableProperty] private string _redeemCode = string.Empty;

    public bool IsSupporter => _supporter.IsSupporter;

    public string SupporterStatusLabel => _supporter.IsSupporter
        ? "Thanks for your support! Your supporter perks are unlocked."
        : "Have a supporter code from Patreon? Redeem it below.";

    partial void OnDonateCycleChanged(string value) => ApplyCycle();

    private void ApplyCycle()
    {
        bool monthly = DonateCycle == "monthly";
        foreach (DonateTierViewModel tier in Tiers)
        {
            tier.SetCycle(monthly);
        }

        OnPropertyChanged(nameof(Tiers));
    }

    [RelayCommand]
    private void Donate() => _dialog.OpenUrl(PatreonUrl);

    [RelayCommand]
    private async Task RedeemAsync()
    {
        if (string.IsNullOrWhiteSpace(RedeemCode))
        {
            return;
        }

        Result<SupporterEntitlement> result = await _supporter.RedeemAsync(RedeemCode.Trim()).ConfigureAwait(true);
        if (result.IsSuccess)
        {
            RedeemCode = string.Empty;
            _bus.Publish(new ToastMessage("Thank you 💚", $"Your {result.Value.Tier} perks are unlocked."));
            OnPropertyChanged(nameof(IsSupporter));
            OnPropertyChanged(nameof(SupporterStatusLabel));
        }
        else
        {
            _bus.Publish(new ToastMessage("Couldn't redeem that", result.Error.Message, ToastKind.Error));
        }
    }
}
