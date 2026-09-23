namespace PSAAS.MVVM;

/// <summary>
/// Base type for view models created by a parent and passed to a child component.
/// The parent remains responsible for explicit disposal of composed children.
/// </summary>
public abstract class ComposedViewModel : ViewModelBase
{
}
