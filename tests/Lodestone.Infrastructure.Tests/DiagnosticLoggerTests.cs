using Lodestone.Application.Messaging;
using Lodestone.Infrastructure.Diagnostics;
using Lodestone.Infrastructure.Messaging;

namespace Lodestone.Infrastructure.Tests;

public class DiagnosticLoggerTests
{
    [Fact]
    public void Describe_combines_the_title_and_body()
    {
        var toast = new ToastMessage("Loader update failed", "Network timeout", ToastKind.Error);

        DiagnosticLogger.Describe(toast).ShouldBe("Loader update failed: Network timeout");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Describe_uses_only_the_title_when_the_body_is_blank(string body)
    {
        var toast = new ToastMessage("You're up to date", body, ToastKind.Success);

        DiagnosticLogger.Describe(toast).ShouldBe("You're up to date");
    }

    [Fact]
    public void Attach_returns_a_subscription_that_can_be_disposed()
    {
        var bus = new InMemoryMessageBus();

        IDisposable subscription = DiagnosticLogger.Attach(bus);

        Should.NotThrow(subscription.Dispose);
    }

    [Fact]
    public void Attach_rejects_a_null_bus()
        => Should.Throw<ArgumentNullException>(() => DiagnosticLogger.Attach(null!));
}
