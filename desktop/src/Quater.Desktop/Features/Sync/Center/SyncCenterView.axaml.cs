using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Quater.Desktop.Core.Sync;

namespace Quater.Desktop.Features.Sync.Center;

public partial class SyncCenterView : UserControl
{
    public static ResolveConflictRequest KeepLocalRequest { get; } = new("demo-conflict", ConflictResolutionChoice.KeepLocal);
    public static ResolveConflictRequest KeepServerRequest { get; } = new("demo-conflict", ConflictResolutionChoice.KeepServer);
    public static ResolveConflictRequest ReloadRequest { get; } = new("demo-conflict", ConflictResolutionChoice.Reload);

    public SyncCenterView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
