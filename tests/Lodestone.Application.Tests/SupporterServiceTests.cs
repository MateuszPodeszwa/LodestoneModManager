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

    private sealed class InMemoryEntitlementStore(StoredEntitlement? seed = null) : IEntitlementStore
    {
        public StoredEntitlement? Current { get; private set; } = seed;

        public Task<StoredEntitlement?> LoadAsync(CancellationToken ct = default)
        {
            Changed?.Invoke(this, EventArgs.Empty);
            return Task.FromResult(Current);
        }

        public Task SaveAsync(StoredEntitlement entitlement, CancellationToken ct = default)
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
    public async Task Redeeming_a_fresh_code_unlocks_permanent_supporter_perks()
    {
        var verifier = Substitute.For<ISupporterCodeVerifier>();
        verifier.Verify("GOOD").Returns(Result.Success(new SupporterCode("patron@example.com", Now)));
        var store = new InMemoryEntitlementStore();
        var service = new SupporterService(verifier, store, new FixedClock(Now));

        Result<SupporterEntitlement> result = await service.RedeemAsync("GOOD");

        result.IsSuccess.ShouldBeTrue();
        service.IsSupporter.ShouldBeTrue();
        service.CanUseBetaChannel.ShouldBeTrue();
        service.CanUseExtraThemes.ShouldBeTrue();
        service.Holder.ShouldBe("patron@example.com");
        store.Current!.Code.ShouldBe("GOOD");
    }

    [Fact]
    public async Task A_code_outside_the_one_hour_window_is_rejected()
    {
        var verifier = Substitute.For<ISupporterCodeVerifier>();
        verifier.Verify("OLD").Returns(Result.Success(new SupporterCode("me", Now.AddHours(-2))));
        var store = new InMemoryEntitlementStore();
        var service = new SupporterService(verifier, store, new FixedClock(Now));

        Result<SupporterEntitlement> result = await service.RedeemAsync("OLD");

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("supporter.expired");
        service.IsSupporter.ShouldBeFalse();
        store.Current.ShouldBeNull();
    }

    [Fact]
    public async Task An_invalid_code_is_rejected_and_stores_nothing()
    {
        var verifier = Substitute.For<ISupporterCodeVerifier>();
        verifier.Verify("BAD").Returns(Result.Failure<SupporterCode>("supporter.invalid", "Invalid code."));
        var store = new InMemoryEntitlementStore();
        var service = new SupporterService(verifier, store, new FixedClock(Now));

        Result<SupporterEntitlement> result = await service.RedeemAsync("BAD");

        result.IsFailure.ShouldBeTrue();
        service.IsSupporter.ShouldBeFalse();
        store.Current.ShouldBeNull();
    }

    [Fact]
    public void A_stored_code_that_still_verifies_grants_supporter_status_on_load()
    {
        // Issued 30 days ago but already activated: the 1-hour window only gates *redeeming*, not holding.
        var verifier = Substitute.For<ISupporterCodeVerifier>();
        verifier.Verify("STORED").Returns(Result.Success(new SupporterCode("me", Now.AddDays(-30))));
        var store = new InMemoryEntitlementStore(new StoredEntitlement("STORED", "me", Now.AddDays(-30)));
        var service = new SupporterService(verifier, store, new FixedClock(Now));

        service.IsSupporter.ShouldBeTrue();
    }

    [Fact]
    public void A_tampered_stored_code_is_not_a_supporter()
    {
        var verifier = Substitute.For<ISupporterCodeVerifier>();
        verifier.Verify("TAMPERED").Returns(Result.Failure<SupporterCode>("supporter.invalid", "bad signature"));
        var store = new InMemoryEntitlementStore(new StoredEntitlement("TAMPERED", "me", Now));
        var service = new SupporterService(verifier, store, new FixedClock(Now));

        service.IsSupporter.ShouldBeFalse();
    }

    [Fact]
    public async Task Revoking_clears_supporter_status()
    {
        var verifier = Substitute.For<ISupporterCodeVerifier>();
        verifier.Verify("GOOD").Returns(Result.Success(new SupporterCode("me", Now)));
        var store = new InMemoryEntitlementStore();
        var service = new SupporterService(verifier, store, new FixedClock(Now));
        await service.RedeemAsync("GOOD");

        await service.RevokeAsync();

        service.IsSupporter.ShouldBeFalse();
        store.Current.ShouldBeNull();
    }
}
