using Lodestone.Domain.Common;

namespace Lodestone.Application.Abstractions;

/// <summary>Finds and validates the Minecraft installation directory.</summary>
public interface IGameLocator
{
    /// <summary>Best-effort auto-detection of <c>.minecraft</c>; failure means "ask the user".</summary>
    Result<string> Detect();

    /// <summary>True when the path looks like a real Minecraft install (and is writable).</summary>
    bool IsValid(string? path);
}
