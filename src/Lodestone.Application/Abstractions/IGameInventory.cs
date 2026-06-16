using Lodestone.Domain;

namespace Lodestone.Application.Abstractions;

/// <summary>
/// Reads what is actually present in the configured Minecraft directory — which base game versions
/// have a profile under <c>versions/</c>. Distinct from <see cref="IGameLocator"/>, which only
/// validates that the folder is a Minecraft install; this enumerates its contents so the UI and the
/// install pipeline can stop guessing the "active version".
/// </summary>
public interface IGameInventory
{
    /// <summary>
    /// Base game versions installed under <c>versions/</c>, newest first. A modded profile counts as
    /// its <c>inheritsFrom</c> base. Empty when nothing is installed or the directory isn't set/valid.
    /// </summary>
    IReadOnlyList<GameVersion> InstalledVersions();

    /// <summary>True when a base profile (vanilla, or a modded one that inherits from it) exists for this version.</summary>
    bool IsVersionInstalled(GameVersion version);
}
