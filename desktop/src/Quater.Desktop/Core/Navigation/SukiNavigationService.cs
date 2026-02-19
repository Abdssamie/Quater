using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;

namespace Quater.Desktop.Core.Navigation;

public sealed class SukiNavigationService(IServiceProvider serviceProvider) : INavigationService
{
    private readonly Dictionary<Type, NavigationItem> _routes = new();
    private readonly ObservableCollection<NavigationItem> _navigationItems = [];

    private ViewModelBase? _currentView;
    public ViewModelBase? CurrentView
    {
        get => _currentView;
        private set
        {
            if (_currentView == value) return;
            _currentView = value;
            CurrentViewChanged?.Invoke(this, value!);
        }
    }

    public IReadOnlyList<NavigationItem> NavigationItems => _navigationItems;

    public event EventHandler<ViewModelBase>? CurrentViewChanged;

    public void RegisterRoute<TViewModel>(NavigationItem item) where TViewModel : ViewModelBase
    {
        var vmType = typeof(TViewModel);
        _routes[vmType] = item;

        if (!_navigationItems.Contains(item))
        {
            _navigationItems.Add(item);
            var sorted = _navigationItems.OrderBy(x => x.Order).ToList();
            _navigationItems.Clear();
            foreach (var navItem in sorted)
                _navigationItems.Add(navItem);
        }
    }

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        NavigateTo(typeof(TViewModel));
    }

    public void NavigateTo(Type viewModelType)
    {
        if (!_routes.TryGetValue(viewModelType, out var item))
            throw new InvalidOperationException($"Route not registered for {viewModelType.Name}");

        NavigateTo(item);
    }

    public void NavigateTo(NavigationItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var viewModel = (ViewModelBase)serviceProvider.GetRequiredService(item.ViewModelType);
        CurrentView = viewModel;
    }
}
