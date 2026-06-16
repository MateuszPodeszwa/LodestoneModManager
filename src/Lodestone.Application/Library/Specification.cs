namespace Lodestone.Application.Library;

/// <summary>
/// A composable predicate (Specification pattern). Concrete specifications describe one rule; they
/// combine with <see cref="And"/> so the library query reads declaratively instead of as a pile of
/// nested <c>if</c>s.
/// </summary>
public abstract class Specification<T>
{
    public static Specification<T> All { get; } = new TrueSpecification<T>();

    public abstract bool IsSatisfiedBy(T candidate);

    public Specification<T> And(Specification<T> other) => new AndSpecification<T>(this, other);

    /// <summary>The identity specification: matches everything. Backs <see cref="All"/> as the neutral starting point for <see cref="And"/> chains.</summary>
    private sealed class TrueSpecification<TItem> : Specification<TItem>
    {
        public override bool IsSatisfiedBy(TItem candidate) => true;
    }

    /// <summary>The logical AND of two specifications: satisfied only when both operands are.</summary>
    private sealed class AndSpecification<TItem>(Specification<TItem> left, Specification<TItem> right)
        : Specification<TItem>
    {
        public override bool IsSatisfiedBy(TItem candidate)
            => left.IsSatisfiedBy(candidate) && right.IsSatisfiedBy(candidate);
    }
}
