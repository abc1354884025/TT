using System;
using System.Collections.Generic;

/// <summary>
/// 响应式属性。值变化时自动通知订阅者。零反射，IL2CPP + 热更安全。
/// </summary>
public class BindableProperty<T>
{
    private T _value;

    public BindableProperty() : this(default) { }
    public BindableProperty(T initialValue) { _value = initialValue; }

    public T Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                OnChanged?.Invoke(value);
            }
        }
    }

    public event Action<T> OnChanged;

    /// <summary>强制推送当前值（绑定初始化时用）</summary>
    public void Refresh() => OnChanged?.Invoke(_value);

    /// <summary>清除所有订阅</summary>
    public void Clear() => OnChanged = null;

    public static implicit operator T(BindableProperty<T> prop) => prop != null ? prop.Value : default;
    public override string ToString() => _value?.ToString() ?? "null";
}

public static class BindablePropertyExtensions
{
    /// <summary>订阅并立即用当前值回调一次，返回取消订阅的 Action</summary>
    public static Action SubscribeAndRefresh<T>(this BindableProperty<T> prop, Action<T> callback)
    {
        prop.OnChanged += callback;
        callback(prop.Value);
        return () => prop.OnChanged -= callback;
    }
}
