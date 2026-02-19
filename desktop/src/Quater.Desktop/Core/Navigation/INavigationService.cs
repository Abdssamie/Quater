namespace Quater.Desktop.Core.Navigation;

public interface INavigationService
{
    ViewModelBase? CurrentView { get; }
    IReadOnlyList<NavigationItem> NavigationItems { get; }

    event EventHandler<ViewModelBase>? CurrentViewChanged;

    void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;
    void NavigateTo(Type viewModelType);
    void NavigateTo(NavigationItem item);

    void RegisterRoute<TViewModel>(NavigationItem item) where TViewModel : ViewModelBase;
}
