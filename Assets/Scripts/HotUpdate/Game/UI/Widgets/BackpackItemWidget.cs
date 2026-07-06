using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包网格中的物品渲染器。
/// 根据 PlacedItem 的形状和位置渲染成彩色方块组。
/// Phase 2 仅静态渲染，Phase 3 加拖拽。
/// </summary>
public class BackpackItemWidget : MonoBehaviour
{
    [SerializeField] private Image _background;
    [SerializeField] private RarityBadgeWidget _rarityBadge;

    public PlacedItem PlacedItem { get; private set; }

    /// <summary>初始化物品显示</summary>
    public void Init(PlacedItem placedItem, float cellSize)
    {
        PlacedItem = placedItem;
        var shape = placedItem.RotatedShape;

        // 调整自身 RectTransform 覆盖形状所有格子
        var rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(shape.Width * cellSize, shape.Height * cellSize);
        rt.pivot = new Vector2(0, 1); // 左上锚点

        if (_background)
            _background.color = GetColorByRarity(placedItem.ItemData.Rarity);

        if (_rarityBadge)
            _rarityBadge.SetRarity(placedItem.ItemData.Rarity);

        // 按形状裁剪 Image（用 Mask 或直接调整子图）
        // 简单方案：在子 Grid 中生成小块
        RenderShapeBlocks(shape, cellSize);
    }

    private void RenderShapeBlocks(ItemShape shape, float cellSize)
    {
        // 为形状的每个格子创建一个彩色小块
        for (int sx = 0; sx < shape.Width; sx++)
        {
            for (int sy = 0; sy < shape.Height; sy++)
            {
                if (!shape.Cells[sx, sy]) continue;

                var block = new GameObject("Cell", typeof(Image));
                block.transform.SetParent(transform, false);
                var img = block.GetComponent<Image>();
                img.color = _background ? _background.color : Color.white;

                var rt = block.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(sx * cellSize, -sy * cellSize);
                rt.sizeDelta = new Vector2(cellSize, cellSize);
            }
        }
    }

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
