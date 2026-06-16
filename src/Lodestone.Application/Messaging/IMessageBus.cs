namespace Lodestone.Application.Messaging;

/// <summary>
/// A minimal in-process publish/subscribe bus (lightweight Mediator). Used to surface cross-cutting
/// signals — most visibly toast notifications — without view models holding references to each other.
/// </summary>
public interface IMessageBus
{
    void Publish<TMessage>(TMessage message);

    IDisposable Subscribe<TMessage>(Action<TMessage> handler);
}

/// <summary>A request to show a transient toast (see the design's bottom-right notifications).</summary>
public sealed record ToastMessage(string Title, string Body, ToastKind Kind = ToastKind.Success);

public enum ToastKind
{
    Success = 0,
    Info,
    Warning,
    Error,
}
