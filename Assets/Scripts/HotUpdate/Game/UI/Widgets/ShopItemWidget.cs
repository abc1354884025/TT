using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店物品卡片——显示物品图标/名称/价格/稀有度边框，点击购买。
/// 组件自动从子对象查找，也支持 Inspector 拖拽覆盖。
/// </summary>
public class ShopItemWidget : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _priceText;
    [SerializeField] private RarityBadgeWidget _rarityBadge;
    [SerializeField] private Button _buyButton;

    public ItemData ItemData { get; private set; }
    public event System.Action<ItemData> OnBuyClicked;

    private void Awake()
    {
        // 自动查找（优先 Inspector 拖拽的值）
        if (!_icon) _icon = transform.Find("Icon")?.GetComponent<Image>();
        if (!_nameText) _nameText = transform.Find("NameText")?.GetComponent<Text>();
        if (!_priceText) _priceText = transform.Find("PriceText")?.GetComponent<Text>();
        if (!_buyButton) _buyButton = GetComponent<Button>();
        if (!_rarityBadge) _rarityBadge = GetComponent<RarityBadgeWidget>();
    }

    public void Init(ItemData data, bool canAfford)
    {
        ItemData = data;
        if (_nameText) _nameText.text = data.Name;
        if (_priceText) _priceText.text = $"{data.BuyPrice}G";
        if (_icon && !string.IsNullOrEmpty(data.IconPath))
            _icon.sprite = ResourceManager.LoadSprite(data.IconPath);
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
