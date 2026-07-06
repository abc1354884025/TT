using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 商店物品卡片——显示物品图标/名称/价格/稀有度边框。
/// 点击购买。
/// </summary>
public class ShopItemWidget : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _priceText;
    [SerializeField] private RarityBadgeWidget _rarityBadge;
    [SerializeField] private Button _buyButton;

    public ItemData ItemData { get; private set; }
    public event System.Action<ItemData> OnBuyClicked;

    public void Init(ItemData data, bool canAfford)
    {
        ItemData = data;
        if (_nameText) _nameText.text = data.Name;
        if (_priceText) _priceText.text = $"{data.BuyPrice}G";
        if (_rarityBadge) _rarityBadge.SetRarity(data.Rarity);
        if (_buyButton)
        {
            _buyButton.interactable = canAfford;
            _buyButton.onClick.RemoveAllListeners();
            _buyButton.onClick.AddListener(() => OnBuyClicked?.Invoke(data));
        }
    }

    public void SetAffordable(bool canAfford)
    {
        if (_buyButton) _buyButton.interactable = canAfford;
    }
}
