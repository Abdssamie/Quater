using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SukiUI.Controls;

namespace Quater.Desktop.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _complianceRate = "94.2%";
    
    [ObservableProperty]
    private string _samplesThisWeek = "128";
    
    [ObservableProperty]
    private string _pendingAlerts = "3";

    [ObservableProperty]
    private ObservableCollection<DashboardStat> _stats;

    [ObservableProperty]
    private ObservableCollection<RecentSample> _recentSamples;

    public DashboardViewModel()
    {
        Stats = new ObservableCollection<DashboardStat>
        {
            new("Total Samples", "1,248", "M18,17L21,22H3L6,17H18M18,17L14,5H10L6,17H18M15,4H9L8,4H9L12,1L15,4Z"),
            new("Compliance Rate", "94.2%", "M12 2C6.5 2 2 6.5 2 12S6.5 22 12 22 22 17.5 22 12 17.5 2 12 2M10 17L5 12L6.41 10.59L10 14.17L17.59 6.58L19 8L10 17Z", true),
            new("Critical Alerts", "3", "M13 14H11V9H13M13 18H11V16H13M1 21H23L12 2L1 21Z", false, true),
            new("Active Labs", "4", "M12,7V3H2V21H22V7H12M6,19H4V17H6V19M6,15H4V13H6V15M6,11H4V9H6V11M6,7H4V5H6V7M10,19H8V17H10V19M10,15H8V13H10V15M10,11H8V9H10V11M10,7H8V5H10V7M20,19H12V17H20V19M20,15H12V13H20V15M20,11H12V9H20V11Z")
        };

        RecentSamples = new ObservableCollection<RecentSample>
        {
            new("SMP-2026-001", "River Source A", "Phosphate", "0.4 mg/L", "Compliant"),
            new("SMP-2026-002", "Treatment Plant B", "Turbidity", "1.2 NTU", "Compliant"),
            new("SMP-2026-003", "Well #4", "Nitrates", "55 mg/L", "Non-Compliant"),
            new("SMP-2026-004", "River Source A", "pH", "7.2", "Compliant"),
            new("SMP-2026-005", "Reservoir C", "E. Coli", "0 CFU", "Compliant"),
        };
    }
}

public record DashboardStat(string Title, string Value, string Icon, bool IsGood = false, bool IsBad = false);
public record RecentSample(string Id, string Location, string Parameter, string Result, string Status);
