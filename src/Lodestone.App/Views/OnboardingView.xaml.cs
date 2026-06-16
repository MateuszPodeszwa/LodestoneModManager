using System.Windows.Controls;

namespace Lodestone.App.Views;

/// <summary>Code-behind for the first-run onboarding overlay. It only initializes the XAML;
/// all behaviour lives in OnboardingViewModel.</summary>
public partial class OnboardingView : UserControl
{
    public OnboardingView() => InitializeComponent();
}
