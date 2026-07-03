using System;

/// <summary>
/// ViewModel 基类。提供属性变化通知能力。
/// 子类用 BindableProperty&lt;T&gt; 做字段，或手动调用 RaisePropertyChanged。
/// 热更层的 ViewModel 继承此类。
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event Action<string> PropertyChanged;

    protected void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(propertyName);
    }

    /// <summary>将 BindableProperty 的变化转发为 PropertyChanged 事件</summary>
    protected void ForwardProperty<T>(string propertyName, BindableProperty<T> property)
    {
        property.OnChanged += _ => RaisePropertyChanged(propertyName);
    }

    public virtual void Dispose()
    {
        PropertyChanged = null;
    }
}
