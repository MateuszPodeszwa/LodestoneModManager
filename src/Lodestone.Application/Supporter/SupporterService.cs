using Lodestone.Application.Abstractions;
using Lodestone.Domain.Common;

namespace Lodestone.Application.Supporter;

/// <summary>
/// Coordinates redeeming and querying supporter status. Verification is fully offline (signed codes):
/// a code is valid to <em>activate</em> for one hour after it was issued, but once activated the status
/// is permanent. Held status is re-derived from the stored signed code (its signature is re-verified),
/// so it survives restarts yet can't be forged by editing the saved file.
/// </summary>
public sealed class SupporterService
{
    /// <summary>How long a freshly issued code may be redeemed before it must be regenerated.</summary>
    public static readonly TimeSpan ActivationWindow = TimeSpan.FromHours(1);

    private readonly ISupporterCodeVerifier _verifier;
    private readonly IEntitlementStore _store;
    private readonly IClock _clock;

    private bool? _isSupporter; // cached; invalidated whenever the store changes

    public SupporterService(ISupporterCodeVerifier verifier, IEntitlementStore store, IClock clock)
    {
        _verifier = verifier;
        _store = store;
        _clock = clock;
        _store.Changed += (_, e) =>
        {
            _isSupporter = null;
            Changed?.Invoke(this, e);
        };
    }

    /// <summary>Raised when supporter status may have changed (redeem, revoke, or load).</summary>
    public event EventHandler? Changed;

    /// <summary>True when a stored code is present and its signature still verifies.</summary>
    public bool IsSupporter
    {
        get
        {
            _isSupporter ??= Evaluate();
            return _isSupporter.Value;
        }
    }

    /// <summary>The patron's handle from the active entitlement, or null when not a supporter.</summary>
    public string? Holder => IsSupporter ? _store.Current?.Holder : null;

    /// <summary>Whether the beta/early-access update channel may be selected (a supporter perk).</summary>
    public bool CanUseBetaChannel => IsSupporter;

    /// <summary>Whether the supporter-exclusive accent themes are available (a supporter perk).</summary>
    public bool CanUseExtraThemes => IsSupporter;

    /// <summary>Validates a code and, if its signature is valid and it's within the 1-hour activation
    /// window, stores it and grants permanent supporter status.</summary>
    public async Task<Result<SupporterEntitlement>> RedeemAsync(string code, CancellationToken ct = default)
    {
        Result<SupporterCode> verified = _verifier.Verify(code);
        if (verified.IsFailure)
        {
            return Result.Failure<SupporterEntitlement>(verified.Error);
        }

        SupporterCode claims = verified.Value;
        if (_clock.UtcNow > claims.IssuedAt + ActivationWindow)
        {
            return Result.Failure<SupporterEntitlement>("supporter.expired",
                "This code has expired — generate a fresh one on the website (codes last one hour).");
        }

        var stored = new StoredEntitlement(code.Trim(), claims.Holder, _clock.UtcNow);
        await _store.SaveAsync(stored, ct).ConfigureAwait(false);
        _isSupporter = true;
        return new SupporterEntitlement(stored.Holder, stored.ActivatedAt);
    }

    public Task RevokeAsync(CancellationToken ct = default)
    {
        _isSupporter = false;
        return _store.ClearAsync(ct);
    }

    private bool Evaluate()
    {
        StoredEntitlement? current = _store.Current;
        return current is not null && _verifier.Verify(current.Code).IsSuccess;
    }
}
