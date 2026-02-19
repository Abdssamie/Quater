using CommunityToolkit.Mvvm.ComponentModel;
using Quater.Shared.Models;

namespace Quater.Desktop.Core.State;

public sealed partial class AppState : ObservableObject
{
    [ObservableProperty]
    private bool _isSyncing;
    
    [ObservableProperty]
    private bool _isOffline;
    
    [ObservableProperty]
    private string _connectionStatus = "Connected";
    
    [ObservableProperty]
    private string _lastSyncTime = "Never";
    
    [ObservableProperty]
    private User? _currentUser;
    
    [ObservableProperty]
    private int _pendingSyncCount;
    
    [ObservableProperty]
    private string _currentLabName = string.Empty;
    
    public void SetConnected()
    {
        IsOffline = false;
        ConnectionStatus = "Connected";
        LastSyncTime = DateTime.Now.ToString("HH:mm");
    }
    
    public void SetDisconnected()
    {
        IsOffline = true;
        ConnectionStatus = "Disconnected";
    }
    
    public void StartSync()
    {
        IsSyncing = true;
    }
    
    public void EndSync(int pendingCount = 0)
    {
        IsSyncing = false;
        PendingSyncCount = pendingCount;
        LastSyncTime = DateTime.Now.ToString("HH:mm");
    }
}
