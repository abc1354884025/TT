using System;

/// <summary>
/// 属性变化通知接口。不依赖 System.ComponentModel，IL2CPP 安全。
/// </summary>
public interface INotifyPropertyChanged
{
    event Action<string> PropertyChanged;
}
