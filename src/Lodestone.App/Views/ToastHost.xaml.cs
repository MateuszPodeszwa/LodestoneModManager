using System.Windows.Controls;

namespace Lodestone.App.Views;

/// <summary>Code-behind for the toast notification overlay. It only initializes the XAML;
/// the transient toasts are driven by ToastsViewModel.</summary>
public partial class ToastHost : UserControl
{
    public ToastHost() => InitializeComponent();
}
