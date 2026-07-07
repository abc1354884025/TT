using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 背包网格中的物品渲染器——支持点击拖拽移动位置。
/// </summary>
public class BackpackItemWidget : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image _background;
    [SerializeField] private RarityBadgeWidget _rarityBadge;

    public PlacedItem PlacedItem { get; private set; }
    private BackpackGridWidget _grid;
    private PrepareViewModel _vm;

    public void Init(PlacedItem placedItem, float cellSize, BackpackGridWidget grid = null, PrepareViewModel vm = null)
    {
        PlacedItem = placedItem;
        _grid = grid;
        _vm = vm;
        Debug.Log($"[GridItem] Init {placedItem.ItemData.Name} @ ({placedItem.GridX},{placedItem.GridY}), hasBg={_background != null}, hasImg={GetComponent<Image>() != null}");

        var shape = placedItem.RotatedShape;
        var rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(shape.Width * cellSize, shape.Height * cellSize);
        rt.pivot = new Vector2(0, 1);

        if (_background)
            _background.color = GetColorByRarity(placedItem.ItemData.Rarity);
        if (_rarityBadge)
            _rarityBadge.SetRarity(placedItem.ItemData.Rarity);

        RenderShapeBlocks(shape, cellSize);

        // 确保有 Image 接收射线（预制体通常已有，没有则取现有组件）
        if (!_background)
            _background = GetComponent<Image>();
        if (_background)
            _background.raycastTarget = true;
    }

    private void RenderShapeBlocks(ItemShape shape, float cellSize)
    {
        foreach (Transform child in transform) Destroy(child.gameObject);

        for (int sx = 0; sx < shape.Width; sx++)
        {
            for (int sy = 0; sy < shape.Height; sy++)
            {
                if (!shape.Cells[sx, sy]) continue;
                var block = new GameObject("Cell", typeof(Image));
                block.transform.SetParent(transform, false);
                block.GetComponent<Image>().color = _background ? _background.color : Color.white;
                block.GetComponent<Image>().raycastTarget = false;

                var rt = block.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(sx * cellSize, -sy * cellSize);
                rt.sizeDelta = new Vector2(cellSize, cellSize);
            }
        }
    }

    // ====== 拖拽 ======

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"[GridItem] OnBeginDrag item={PlacedItem?.ItemData?.Name}, vm={_vm != null}, ddm={DragDropManager.Instance != null}");
        if (PlacedItem == null || DragDropManager.Instance == null) return;
        if (_grid == null) _grid = GetComponentInParent<BackpackGridWidget>();

        DragDropManager.Instance.BeginDragFromGrid(this, _vm, _grid);
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log($"[BackpackItemWidget] OnEndDrag 触发, item={PlacedItem?.ItemData?.Name}");
        DragDropManager.Instance?.EndDrag();
    }

    // ====== 辅助 ======

    private Color GetColorByRarity(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => new Color(0.5f, 0.5f, 0.5f, 0.8f),
            ItemRarity.Uncommon => new Color(0.2f, 0.7f, 0.2f, 0.8f),
            ItemRarity.Rare => new Color(0.2f, 0.4f, 1.0f, 0.8f),
            ItemRarity.Epic => new Color(0.6f, 0.2f, 1.0f, 0.8f),
            ItemRarity.Legendary => new Color(1.0f, 0.65f, 0.1f, 0.9f),
            _ => Color.gray
        };
    }
}
