using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// UI 面板抽象基类。所有面板（包括热更层的）都继承此类。
///
/// 生命周期：OnInit → OnOpen → OnShow ⇄ OnHide → OnClose
///
/// public class ShopPanel : UIPanel
/// {
///     protected override void OnOpen(object data) { /* 绑定 VM */ }
///     protected override void OnClose() { /* 解绑 VM */ }
/// }
/// </summary>
[RequireComponent(typeof(RectTransform))]
public abstract class UIPanel : MonoBehaviour
{
    [Header("面板设置")]
    [SerializeField] private string _panelId;
    [SerializeField] private UILayer _layer = UILayer.Normal;
    [SerializeField] private bool _cacheOnClose = true;

    [Header("自动绑定")]
    [SerializeField] protected UIComponentBinding[] AutoBindings;

    public string PanelId
    {
        get => string.IsNullOrEmpty(_panelId) ? GetType().Name : _panelId;
        protected set => _panelId = value;
    }
    public UILayer Layer => _layer;
    public bool CacheOnClose => _cacheOnClose;
    public bool IsOpen { get; private set; }
    public bool IsVisible { get; private set; }
    public object UserData { get; private set; }
    public bool IsInitialized { get; private set; }

    // --- 生命周期 ---
    protected virtual IEnumerator OnInit() { yield break; }
    protected virtual void OnOpen(object data) { }
    protected virtual void OnShow() { }
    protected virtual void OnHide() { }
    protected virtual void OnClose() { }
    public virtual void OnRefresh() { }

    // --- Internal（仅供 UIManager 调用）---
    internal IEnumerator InitCoroutine() { yield return OnInit(); IsInitialized = true; }

    internal void OpenInternal(object data)
    {
        UserData = data; IsOpen = true;
        ApplyAutoBindings();
        OnOpen(data);
    }

    internal void ShowInternal() { IsVisible = true; gameObject.SetActive(true); transform.SetAsLastSibling(); OnShow(); }
    internal void HideInternal() { IsVisible = false; OnHide(); gameObject.SetActive(false); }

    internal void CloseInternal()
    {
        IsOpen = false; IsVisible = false;
        OnClose();
    }

    // --- 自动绑定 ---
    private void ApplyAutoBindings()
    {
        if (AutoBindings == null || AutoBindings.Length == 0) return;
        var type = GetType();
        foreach (var b in AutoBindings)
        {
            if (!b.BindTarget || string.IsNullOrEmpty(b.FieldName)) continue;
            var field = GetFieldRecursive(type, b.FieldName);
            if (field != null) field.SetValue(this, b.BindTarget);
            else Debug.LogWarning($"[UIPanel] {GetType().Name} 自动绑定失败: {b.FieldName}");
        }
    }

    private FieldInfo GetFieldRecursive(Type type, string name)
    {
        while (type != null && type != typeof(UIPanel) && type != typeof(MonoBehaviour))
        {
            var f = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null) return f;
            type = type.BaseType;
        }
        return null;
    }
}
