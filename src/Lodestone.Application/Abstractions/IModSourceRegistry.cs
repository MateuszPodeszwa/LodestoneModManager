namespace Lodestone.Application.Abstractions;

/// <summary>
/// Chooses which <see cref="IModSource"/>s are active and in what order, honouring the
/// "use CurseForge as fallback" setting. Factory + Strategy: callers ask the registry rather than
/// newing-up sources or knowing the selection rules.
/// </summary>
public interface IModSourceRegistry
{
    /// <summary>The preferred, configured source (Modrinth by default).</summary>
    IModSource Primary { get; }

    /// <summary>
    /// All active, configured sources in priority order. When the fallback setting is on and
    /// CurseForge is configured, it appears after Modrinth; otherwise only the primary is returned.
    /// </summary>
    IReadOnlyList<IModSource> GetActiveSources();

    /// <summary>Finds a source by its <see cref="IModSource.Name"/>, or null.</summary>
    IModSource? Find(string name);
}
