using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 物品栏列表项——显示名称、稀有度颜色。
/// 实现拖拽接口，通过事件通知父面板处理拖放逻辑。
/// </summary>
public class InventoryItemWidget : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private Image _bg;
    [SerializeField] private Button _button;

    public ItemData ItemData { get; private set; }

    /// <summary>拖拽开始时触发（由 PreparePanel 订阅）</summary>
    public event System.Action<ItemData> OnBeginDragItem;

    /// <summary>拖拽结束时触发</summary>
    public event System.Action OnEndDragItem;

    private static readonly Color[] RarityBgColors = new[]
    {
        new Color(0.25f, 0.25f, 0.30f),
        new Color(0.15f, 0.35f, 0.15f),
        new Color(0.15f, 0.25f, 0.45f),
        new Color(0.30f, 0.15f, 0.45f),
        new Color(0.45f, 0.35f, 0.05f),
    };

    private void Awake()
    {
        if (!_nameText) _nameText = GetComponentInChildren<TMP_Text>();
        if (!_bg) _bg = GetComponent<Image>();
        if (!_button) _button = GetComponent<Button>();
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

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (ItemData == null) return;
        // 禁用父 ScrollRect 滚动，避免拖拽被拦截
        DisableParentScroll(true);
        OnBeginDragItem?.Invoke(ItemData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // DragDropManager.Update() 自动处理
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        DisableParentScroll(false);
        OnEndDragItem?.Invoke();
    }

    private void DisableParentScroll(bool disable)
    {
        var sr = GetComponentInParent<ScrollRect>();
        if (sr) sr.enabled = !disable;
    }
}
