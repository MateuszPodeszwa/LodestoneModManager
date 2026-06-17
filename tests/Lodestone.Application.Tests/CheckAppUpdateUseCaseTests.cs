using Lodestone.Application.Abstractions;
using Lodestone.Application.Settings;
using Lodestone.Application.Supporter;
using Lodestone.Application.UseCases;
using Lodestone.Domain.Common;
using NSubstitute;

namespace Lodestone.Application.Tests;

public class CheckAppUpdateUseCaseTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-17T00:00:00Z");

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    // Records the channel it was asked to check and returns a canned result.
    private sealed class CapturingAppUpdater : IAppUpdater
    {
        public UpdateChannel? LastChannel { get; private set; }
        public UpdateCheckResult Canned { get; set; } = new(false, "0.1.2", null);

        public string CurrentVersion => "0.1.2";

        public Task<Result<UpdateCheckResult>> CheckAsync(UpdateChannel channel, CancellationToken ct = default)
        {
            LastChannel = channel;
            return Task.FromResult(Result.Success(Canned));
        }

        public Task<Result> DownloadAsync(IProgress<int>? progress = null, CancellationToken ct = default) => Task.FromResult(Result.Success());

        public Result ApplyAndRestart() => Result.Success();

        public Result ApplyOnExit() => Result.Success();
    }

    private static SupporterService SupporterWho(bool isSupporter)
    {
        var verifier = Substitute.For<ISupporterCodeVerifier>();
        var store = Substitute.For<IEntitlementStore>();
        if (isSupporter)
        {
            store.Current.Returns(new StoredEntitlement("CODE", "me", Now));
            verifier.Verify("CODE").Returns(Result.Success(new SupporterCode("me", Now)));
        }
        else
        {
            store.Current.Returns((StoredEntitlement?)null);
        }

        return new SupporterService(verifier, store, new FixedClock(Now));
    }

    [Theory]
    [InlineData(UpdateChannel.Beta, true, UpdateChannel.Beta)]      // supporter with early access on → beta
    [InlineData(UpdateChannel.Beta, false, UpdateChannel.Stable)]   // stale Beta pref, no longer a supporter → stable
    [InlineData(UpdateChannel.Stable, true, UpdateChannel.Stable)]  // supporter, early access off → stable
    [InlineData(UpdateChannel.Stable, false, UpdateChannel.Stable)] // plain non-supporter → stable
    public async Task Checks_only_the_channel_the_user_is_entitled_to(UpdateChannel saved, bool isSupporter, UpdateChannel expected)
    {
        var updater = new CapturingAppUpdater();
        var settings = Substitute.For<ISettingsStore>();
        settings.Current.Returns(new LodestoneSettings { UpdateChannel = saved });
        var useCase = new CheckAppUpdateUseCase(updater, settings, SupporterWho(isSupporter));

        await useCase.ExecuteAsync();

        useCase.EffectiveChannel.ShouldBe(expected);
        updater.LastChannel.ShouldBe(expected);
    }

    [Fact]
    public async Task Passes_the_updater_result_through_including_the_early_access_flag()
    {
        var updater = new CapturingAppUpdater
        {
            Canned = new UpdateCheckResult(UpdateAvailable: true, CurrentVersion: "0.1.2", LatestVersion: "0.1.3-beta.1", IsPrerelease: true),
        };
        var settings = Substitute.For<ISettingsStore>();
        settings.Current.Returns(new LodestoneSettings { UpdateChannel = UpdateChannel.Beta });
        var useCase = new CheckAppUpdateUseCase(updater, settings, SupporterWho(true));

        Result<UpdateCheckResult> result = await useCase.ExecuteAsync();

        result.IsSuccess.ShouldBeTrue();
        result.Value.UpdateAvailable.ShouldBeTrue();
        result.Value.LatestVersion.ShouldBe("0.1.3-beta.1");
        result.Value.IsPrerelease.ShouldBeTrue();
    }
}
