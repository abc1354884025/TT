using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 物品栏列表项——显示名称、稀有度颜色、点击放入背包。
/// </summary>
public class InventoryItemWidget : MonoBehaviour
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private Image _bg;
    [SerializeField] private Button _button;

    public ItemData ItemData { get; private set; }
    public event System.Action<ItemData> OnClicked;

    private static readonly Color[] RarityBgColors = new[]
    {
        new Color(0.25f, 0.25f, 0.30f),  // Common
        new Color(0.15f, 0.35f, 0.15f),  // Uncommon
        new Color(0.15f, 0.25f, 0.45f),  // Rare
        new Color(0.30f, 0.15f, 0.45f),  // Epic
        new Color(0.45f, 0.35f, 0.05f),  // Legendary
    };

    private void Awake()
    {
        if (!_nameText) _nameText = GetComponentInChildren<TMP_Text>();
        if (!_bg) _bg = GetComponent<Image>();
        if (!_button) _button = GetComponent<Button>();
        if (_button) _button.onClick.AddListener(() => OnClicked?.Invoke(ItemData));
    }

    public void Init(ItemData data)
    {
        ItemData = data;
        if (_nameText) _nameText.text = data.Name;
        if (_bg)
        {
            int idx = (int)data.Rarity;
            _bg.color = idx < RarityBgColors.Length ? RarityBgColors[idx] : RarityBgColors[0];
        }
    }
}
