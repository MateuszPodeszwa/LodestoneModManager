using Lodestone.Application.Abstractions;
using Lodestone.Domain.Common;

namespace Lodestone.Application.Supporter;

/// <summary>
/// Coordinates redeeming and querying supporter status. Verification is offline (signed codes);
/// this service adds expiry checks and persistence, and exposes the cosmetic perks that are unlocked.
/// </summary>
public sealed class SupporterService
{
    private readonly ISupporterCodeVerifier _verifier;
    private readonly IEntitlementStore _store;
    private readonly IClock _clock;

    public SupporterService(ISupporterCodeVerifier verifier, IEntitlementStore store, IClock clock)
    {
        _verifier = verifier;
        _store = store;
        _clock = clock;
    }

    /// <summary>True when a valid, non-expired entitlement is held.</summary>
    public bool IsSupporter
    {
        get
        {
            SupporterEntitlement? current = _store.Current;
            return current is not null && !current.IsExpired(_clock);
        }
    }

    /// <summary>Whether the beta update channel may be selected (a supporter perk).</summary>
    public bool CanUseBetaChannel => IsSupporter;

    /// <summary>Whether extra accent themes are available (a supporter perk).</summary>
    public bool CanUseExtraThemes => IsSupporter;

    /// <summary>Validates a code and, if valid and unexpired, stores the entitlement.</summary>
    public async Task<Result<SupporterEntitlement>> RedeemAsync(string code, CancellationToken ct = default)
    {
        Result<SupporterEntitlement> verified = _verifier.Verify(code);
        if (verified.IsFailure)
        {
            return verified;
        }

        SupporterEntitlement entitlement = verified.Value;
        if (entitlement.IsExpired(_clock))
        {
            return Result.Failure<SupporterEntitlement>("supporter.expired", "This code has expired.");
        }

        await _store.SaveAsync(entitlement, ct).ConfigureAwait(false);
        return entitlement;
    }

    public Task RevokeAsync(CancellationToken ct = default) => _store.ClearAsync(ct);
}
