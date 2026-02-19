using CommunityToolkit.Mvvm.ComponentModel;

namespace Quater.Desktop.Core;

public abstract class ViewModelBase : ObservableObject
{
    public virtual Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
}
