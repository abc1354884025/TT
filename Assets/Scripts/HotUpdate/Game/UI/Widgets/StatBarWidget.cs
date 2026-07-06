using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 属性条——显示图标+文字+数值。用于 HP/ATK/DEF 面板。
/// </summary>
public class StatBarWidget : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private Text _label;
    [SerializeField] private Text _valueText;

    private string _labelStr;

    public void Init(string label, Sprite icon = null)
    {
        _labelStr = label;
        if (_label) _label.text = label;
        if (_icon && icon) _icon.sprite = icon;
    }

    public void SetValue(int value)
    {
        if (_valueText) _valueText.text = value.ToString();
    }
}
