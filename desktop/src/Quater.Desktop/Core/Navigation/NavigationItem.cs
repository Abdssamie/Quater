using Avalonia.Media;

namespace Quater.Desktop.Core.Navigation;

public sealed record NavigationItem(
    string Label,
    string IconPath,
    Type ViewModelType,
    int Order = 0,
    bool IsVisible = true
);
