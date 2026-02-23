using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Quater.Desktop.Core.Splash;

public sealed partial class SplashWindow : SukiUI.Controls.SukiWindow
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
