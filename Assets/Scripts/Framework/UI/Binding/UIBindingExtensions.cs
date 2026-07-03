using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 绑定扩展方法。一行代码绑定 UI 组件到 BindableProperty。
/// 所有方法返回取消订阅的 Action，在 Panel.OnClose 中统一调用。
///
/// 热更层可直接使用（此类在 AOT 程序集中）。
/// </summary>
public static class UIBindingExtensions
{
    #region TMP_Text

    public static Action BindTo(this TMP_Text text, BindableProperty<string> prop)
    {
        void H(string v) { if (text) text.text = v ?? ""; }
        prop.OnChanged += H; prop.Refresh();
        return () => prop.OnChanged -= H;
    }

    public static Action BindTo<T>(this TMP_Text text, BindableProperty<T> prop)
    {
        void H(T v) { if (text) text.text = v?.ToString() ?? ""; }
        prop.OnChanged += H; prop.Refresh();
        return () => prop.OnChanged -= H;
    }

    #endregion

    #region Text (UGUI)

    public static Action BindTo(this Text text, BindableProperty<string> prop)
    {
        void H(string v) { if (text) text.text = v ?? ""; }
        prop.OnChanged += H; prop.Refresh();
        return () => prop.OnChanged -= H;
    }

    public static Action BindTo<T>(this Text text, BindableProperty<T> prop)
    {
        void H(T v) { if (text) text.text = v?.ToString() ?? ""; }
        prop.OnChanged += H; prop.Refresh();
        return () => prop.OnChanged -= H;
    }

    #endregion

    #region Image

    public static Action BindTo(this Image img, BindableProperty<Sprite> prop)
    {
        void H(Sprite v) { if (img) img.sprite = v; }
        prop.OnChanged += H; prop.Refresh();
        return () => prop.OnChanged -= H;
    }

    public static Action BindFillAmount(this Image img, BindableProperty<float> prop)
    {
        void H(float v) { if (img) img.fillAmount = Mathf.Clamp01(v); }
        prop.OnChanged += H; prop.Refresh();
        return () => prop.OnChanged -= H;
    }

    public static Action BindColor(this Image img, BindableProperty<Color> prop)
    {
        void H(Color v) { if (img) img.color = v; }
        prop.OnChanged += H; prop.Refresh();
        return () => prop.OnChanged -= H;
    }

    #endregion

    #region Button

    public static Action BindClick(this Button btn, Action onClick)
    {
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => onClick?.Invoke());
        return () => btn.onClick.RemoveAllListeners();
    }

    public static Action BindClick<T>(this Button btn, Action<T> onClick, T param)
    {
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => onClick?.Invoke(param));
        return () => btn.onClick.RemoveAllListeners();
    }

    public static Action BindInteractable(this Button btn, BindableProperty<bool> prop)
    {
        void H(bool v) { if (btn) btn.interactable = v; }
        prop.OnChanged += H; prop.Refresh();
        return () => prop.OnChanged -= H;
    }

    #endregion

    #region Slider

    public static Action BindTo(this Slider slider, BindableProperty<float> prop)
    {
        void H(float v) { if (slider) slider.value = v; }
        prop.OnChanged += H; prop.Refresh();
        return () => prop.OnChanged -= H;
    }

    public static void BindTwoWay(this Slider slider, BindableProperty<float> prop)
    {
        void OnVM(float v) { if (slider) slider.SetValueWithoutNotify(v); }
        prop.OnChanged += OnVM; prop.Refresh();
        slider.onValueChanged.AddListener(v => prop.Value = v);
    }

    #endregion

    #region Toggle

    public static Action BindTo(this Toggle toggle, BindableProperty<bool> prop)
    {
        void H(bool v) { if (toggle) toggle.isOn = v; }
        prop.OnChanged += H; prop.Refresh();
        return () => prop.OnChanged -= H;
    }

    public static void BindTwoWay(this Toggle toggle, BindableProperty<bool> prop)
    {
        void OnVM(bool v) { if (toggle) toggle.SetIsOnWithoutNotify(v); }
        prop.OnChanged += OnVM; prop.Refresh();
        toggle.onValueChanged.AddListener(v => prop.Value = v);
    }

    #endregion

    #region GameObject 可见性

    public static Action BindActive(this GameObject go, BindableProperty<bool> prop)
    {
        void H(bool v) { if (go) go.SetActive(v); }
        prop.OnChanged += H; prop.Refresh();
        return () => prop.OnChanged -= H;
    }

    public static Action BindInactive(this GameObject go, BindableProperty<bool> prop)
    {
        void H(bool v) { if (go) go.SetActive(!v); }
        prop.OnChanged += H; prop.Refresh();
        return () => prop.OnChanged -= H;
    }

    #endregion

    #region TMP_InputField

    public static Action BindTo(this TMP_InputField input, BindableProperty<string> prop)
    {
        void H(string v) { if (input) input.text = v ?? ""; }
        prop.OnChanged += H; prop.Refresh();
        return () => prop.OnChanged -= H;
    }

    #endregion
}
