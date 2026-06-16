using Lodestone.Domain;
using Lodestone.Domain.Common;

namespace Lodestone.Application.Abstractions;

/// <summary>How to resolve a name collision when placing a file.</summary>
public enum DuplicateResolution
{
    /// <summary>Fail with an error if a file of the same name already exists.</summary>
    Fail = 0,

    /// <summary>Replace the existing file (the old one is moved to trash first).</summary>
    Replace,

    /// <summary>Keep both by giving the new file a unique name.</summary>
    KeepBoth,
}

public sealed record PlaceResult(string FileName, long SizeBytes, bool ReplacedExisting);

/// <summary>
/// Places content files into the correct <c>.minecraft</c> subfolder and manages their on-disk
/// enabled/uninstalled state. The vanilla launcher shares one folder per content type across all
/// game versions, so "profiles" are a metadata concept (see <see cref="InstalledContent.GameVersions"/>),
/// not separate folders — which is exactly why compatibility checking matters.
/// </summary>
public interface IContentInstaller
{
    /// <summary>Copies <paramref name="sourceFilePath"/> into the folder for <paramref name="type"/>.</summary>
    Task<Result<PlaceResult>> PlaceAsync(
        string sourceFilePath,
        ContentType type,
        DuplicateResolution onDuplicate = DuplicateResolution.Fail,
        CancellationToken ct = default);

    /// <summary>Enables/disables a file in place (mods toggle the <c>.disabled</c> suffix).</summary>
    Task<Result<string>> SetEnabledAsync(
        ContentType type,
        string fileName,
        bool enabled,
        CancellationToken ct = default);

    /// <summary>Soft-deletes the file (moved to trash) so an accidental removal is recoverable.</summary>
    Task<Result> RemoveAsync(ContentType type, string fileName, CancellationToken ct = default);

    /// <summary>True if a file with this name already exists for the type (enabled or disabled).</summary>
    bool Exists(ContentType type, string fileName);
}
