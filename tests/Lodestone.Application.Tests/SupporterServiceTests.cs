using Lodestone.Application.Abstractions;
using Lodestone.Application.Supporter;
using Lodestone.Domain.Common;
using NSubstitute;

namespace Lodestone.Application.Tests;

public class SupporterServiceTests
{
    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    private sealed class InMemoryEntitlementStore : IEntitlementStore
    {
        public SupporterEntitlement? Current { get; private set; }

        public Task<SupporterEntitlement?> LoadAsync(CancellationToken ct = default) => Task.FromResult(Current);

        public Task SaveAsync(SupporterEntitlement entitlement, CancellationToken ct = default)
        {
            Current = entitlement;
            Changed?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken ct = default)
        {
            Current = null;
            Changed?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public event EventHandler? Changed;
    }

    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-16T00:00:00Z");

    [Fact]
    public async Task Redeeming_a_valid_code_unlocks_supporter_perks()
    {
        var verifier = Substitute.For<ISupporterCodeVerifier>();
        verifier.Verify("GOOD").Returns(Result.Success(new SupporterEntitlement("Supporter", "patron@example.com")));
        var store = new InMemoryEntitlementStore();
        var service = new SupporterService(verifier, store, new FixedClock(Now));

        Result<SupporterEntitlement> result = await service.RedeemAsync("GOOD");

        result.IsSuccess.ShouldBeTrue();
        service.IsSupporter.ShouldBeTrue();
        service.CanUseBetaChannel.ShouldBeTrue();
        service.CanUseExtraThemes.ShouldBeTrue();
        store.Current.ShouldNotBeNull();
    }

    [Fact]
    public async Task An_invalid_code_is_rejected_and_stores_nothing()
    {
        var verifier = Substitute.For<ISupporterCodeVerifier>();
        verifier.Verify("BAD").Returns(Result.Failure<SupporterEntitlement>("supporter.invalid", "Invalid code."));
        var store = new InMemoryEntitlementStore();
        var service = new SupporterService(verifier, store, new FixedClock(Now));

        Result<SupporterEntitlement> result = await service.RedeemAsync("BAD");

        result.IsFailure.ShouldBeTrue();
        service.IsSupporter.ShouldBeFalse();
        store.Current.ShouldBeNull();
    }

    [Fact]
    public async Task An_expired_code_is_rejected()
    {
        var expired = new SupporterEntitlement("Supporter", "patron@example.com", Now.AddDays(-1));
        var verifier = Substitute.For<ISupporterCodeVerifier>();
        verifier.Verify("OLD").Returns(Result.Success(expired));
        var service = new SupporterService(verifier, new InMemoryEntitlementStore(), new FixedClock(Now));

        Result<SupporterEntitlement> result = await service.RedeemAsync("OLD");

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("supporter.expired");
        service.IsSupporter.ShouldBeFalse();
    }
}
