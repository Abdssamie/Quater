using CommunityToolkit.Mvvm.ComponentModel;

namespace Quater.Desktop.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private DashboardViewModel _dashboard;

    public MainViewModel()
    {
        Dashboard = new DashboardViewModel();
    }
}
