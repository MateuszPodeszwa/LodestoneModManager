using Lodestone.Domain.Common;

namespace Lodestone.Domain.Tests;

public class ResultTests
{
    [Fact]
    public void Success_is_successful_and_carries_no_error()
    {
        Result result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Error.ShouldBe(Error.None);
    }

    [Fact]
    public void Failure_carries_the_error_code_and_message()
    {
        Result result = Result.Failure("some.code", "Something went wrong.");

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("some.code");
        result.Error.Message.ShouldBe("Something went wrong.");
    }

    [Fact]
    public void Generic_success_exposes_the_value()
    {
        Result<int> result = Result.Success(42);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Reading_value_of_a_failure_throws()
    {
        Result<int> result = Result.Failure<int>("x", "y");

        Should.Throw<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void A_bare_value_implicitly_becomes_a_success()
    {
        Result<string> result = "hello";

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("hello");
    }

    [Fact]
    public void Map_transforms_a_success_and_propagates_failure()
    {
        Result.Success(10).Map(x => x * 2).Value.ShouldBe(20);

        Result<int> failed = Result.Failure<int>("e", "boom");
        Result<int> mapped = failed.Map(x => x * 2);
        mapped.IsFailure.ShouldBeTrue();
        mapped.Error.Code.ShouldBe("e");
    }

    [Fact]
    public void Bind_chains_results_and_short_circuits_on_failure()
    {
        Result<int> ok = Result.Success(3).Bind(x => Result.Success(x + 1));
        ok.Value.ShouldBe(4);

        Result<int> shortCircuited = Result.Failure<int>("e", "boom").Bind(x => Result.Success(x + 1));
        shortCircuited.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Match_collapses_both_branches()
    {
        Result.Success(5).Match(v => $"ok:{v}", e => $"err:{e.Code}").ShouldBe("ok:5");
        Result.Failure<int>("bad", "m").Match(v => $"ok:{v}", e => $"err:{e.Code}").ShouldBe("err:bad");
    }
}
