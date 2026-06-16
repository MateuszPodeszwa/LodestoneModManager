namespace Lodestone.Domain.Common;

/// <summary>
/// A machine-readable failure descriptor. <see cref="Code"/> is a stable dotted identifier
/// (e.g. <c>game_version.empty</c>) that callers/UI can switch on; <see cref="Message"/> is
/// a human-readable explanation.
/// </summary>
public sealed record Error(string Code, string Message)
{
    /// <summary>The absence of an error (used by successful results).</summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    public override string ToString() => string.IsNullOrEmpty(Code) ? Message : $"{Code}: {Message}";
}
