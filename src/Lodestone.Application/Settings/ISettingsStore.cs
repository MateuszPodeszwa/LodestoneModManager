namespace Lodestone.Application.Settings;

/// <summary>
/// Persists <see cref="LodestoneSettings"/> (Options pattern). Implementations write atomically and
/// raise <see cref="Changed"/> so the UI and services react to changes (Observer).
/// </summary>
public interface ISettingsStore
{
    /// <summary>The current in-memory settings (always normalized).</summary>
    LodestoneSettings Current { get; }

    Task<LodestoneSettings> LoadAsync(CancellationToken ct = default);

    Task SaveAsync(LodestoneSettings settings, CancellationToken ct = default);

    event EventHandler<LodestoneSettings>? Changed;
}
