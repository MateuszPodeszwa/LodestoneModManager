using Lodestone.Domain.Common;

namespace Lodestone.Application.Supporter;

/// <summary>
/// The claims decoded from a verified supporter code. The code is signed by the website/CLI; the app
/// only trusts what the signature covers. <see cref="IssuedAt"/> drives the 1-hour activation window
/// (enforced by <see cref="SupporterService"/>), not the lifetime of the unlocked perks.
/// </summary>
public sealed record SupporterCode(string Holder, DateTimeOffset IssuedAt);

/// <summary>
/// The granted supporter status. Perks are <b>cosmetic/convenience only</b> (badge, accent themes,
/// early-access channel) - core functionality is never gated. Once redeemed it is permanent (there is
/// no recurring expiry); it is only cleared by removing the code or uninstalling.
/// </summary>
public sealed record SupporterEntitlement(string Holder, DateTimeOffset ActivatedAt);

/// <summary>
/// The token persisted on disk: the original signed <see cref="Code"/> plus display metadata. Storing
/// the signed code (rather than a plain "is supporter" flag) is what makes the saved state
/// tamper-resistant - it is re-verified against the embedded public key on load, so editing the file
/// can't fabricate supporter status without a genuinely signed code.
/// </summary>
public sealed record StoredEntitlement(string Code, string Holder, DateTimeOffset ActivatedAt);

/// <summary>Verifies an offline, signed supporter code and returns the claims it encodes.</summary>
public interface ISupporterCodeVerifier
{
    Result<SupporterCode> Verify(string code);
}

/// <summary>Stores the redeemed supporter token and notifies on change (Observer).</summary>
public interface IEntitlementStore
{
    StoredEntitlement? Current { get; }

    Task<StoredEntitlement?> LoadAsync(CancellationToken ct = default);

    Task SaveAsync(StoredEntitlement entitlement, CancellationToken ct = default);

    Task ClearAsync(CancellationToken ct = default);

    event EventHandler? Changed;
}
