using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Quater.Desktop.Features.Auth;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
