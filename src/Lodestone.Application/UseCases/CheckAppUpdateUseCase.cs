using Lodestone.Application.Abstractions;
using Lodestone.Application.Settings;
using Lodestone.Application.Supporter;
using Lodestone.Domain.Common;

namespace Lodestone.Application.UseCases;

/// <summary>
/// Checks for an app self-update on the channel the user is actually entitled to. This is the single
/// gate that keeps early-access (pre-release) builds away from people who shouldn't get them: the Beta
/// channel is used only when the user is a supporter <em>and</em> has early access turned on. A stale
/// <see cref="UpdateChannel.Beta"/> preference left behind by a lapsed supporter is therefore ignored,
/// so a non-supporter is never offered a beta build even if the saved setting still says Beta.
/// </summary>
public sealed class CheckAppUpdateUseCase
{
    private readonly IAppUpdater _updater;
    private readonly ISettingsStore _settings;
    private readonly SupporterService _supporter;

    public CheckAppUpdateUseCase(IAppUpdater updater, ISettingsStore settings, SupporterService supporter)
    {
        _updater = updater;
        _settings = settings;
        _supporter = supporter;
    }

    /// <summary>The channel the app will actually follow right now: Beta only for a supporter who has
    /// early access enabled; otherwise Stable.</summary>
    public UpdateChannel EffectiveChannel =>
        _settings.Current.UpdateChannel == UpdateChannel.Beta && _supporter.IsSupporter
            ? UpdateChannel.Beta
            : UpdateChannel.Stable;

    public Task<Result<UpdateCheckResult>> ExecuteAsync(CancellationToken ct = default)
        => _updater.CheckAsync(EffectiveChannel, ct);
}
