using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Lodestone.App.Services;
using Lodestone.Application.Messaging;

namespace Lodestone.App.ViewModels;

/// <summary>A single toast shown in the host: its title, body and a lowercase <see cref="ToastKind"/> string for styling.</summary>
public sealed class ToastItemViewModel
{
    public ToastItemViewModel(ToastMessage message)
    {
        Title = message.Title;
        Body = message.Body;
        Kind = message.Kind.ToString().ToLowerInvariant();
    }

    public string Title { get; }

    public string Body { get; }

    public string Kind { get; }
}

/// <summary>Listens for <see cref="ToastMessage"/> on the bus and shows transient notifications.</summary>
public sealed class ToastsViewModel
{
    private readonly IUiDispatcher _ui;

    public ToastsViewModel(IMessageBus bus, IUiDispatcher ui)
    {
        _ui = ui;
        bus.Subscribe<ToastMessage>(OnToast);
    }

    public ObservableCollection<ToastItemViewModel> Toasts { get; } = [];

    private void OnToast(ToastMessage message)
    {
        _ui.Post(() =>
        {
            var item = new ToastItemViewModel(message);
            Toasts.Add(item);

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.6) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                Toasts.Remove(item);
            };
            timer.Start();
        });
    }
}
