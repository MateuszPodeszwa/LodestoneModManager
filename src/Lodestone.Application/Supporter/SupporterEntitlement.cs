using Lodestone.Application.Abstractions;
using Lodestone.Domain.Common;

namespace Lodestone.Application.Supporter;

/// <summary>
/// What a redeemed supporter code grants. By design these are <b>cosmetic/convenience only</b>
/// (badge, accent themes, beta channel) — core functionality is never gated.
/// </summary>
public sealed record SupporterEntitlement(string Tier, string Holder, DateTimeOffset? Expires = null)
{
    public bool IsExpired(IClock clock) => Expires is { } e && clock.UtcNow > e;
}

/// <summary>Verifies an offline, signed supporter code and returns the entitlement it encodes.</summary>
public interface ISupporterCodeVerifier
{
    Result<SupporterEntitlement> Verify(string code);
}

/// <summary>Stores the currently-redeemed entitlement and notifies on change (Observer).</summary>
public interface IEntitlementStore
{
    SupporterEntitlement? Current { get; }

    Task<SupporterEntitlement?> LoadAsync(CancellationToken ct = default);

    Task SaveAsync(SupporterEntitlement entitlement, CancellationToken ct = default);

    Task ClearAsync(CancellationToken ct = default);

    event EventHandler? Changed;
}
