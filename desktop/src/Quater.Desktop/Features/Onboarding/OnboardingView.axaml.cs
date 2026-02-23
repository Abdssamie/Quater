using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Quater.Desktop.Features.Onboarding;

public partial class OnboardingView : UserControl
{
    public OnboardingView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
