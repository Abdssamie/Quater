using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Quater.Desktop.ViewModels;

namespace Quater.Desktop.Views;

public partial class SampleListView : UserControl
{
    public SampleListView()
    {
        InitializeComponent();
    }

    protected override async void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        if (DataContext is SampleListViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
