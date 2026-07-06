using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包网格——核心数据结构。
/// 用 int[] 扁平数组存储占位（每个格子存的是 PlacedItem.ItemIndex）。
/// -1 = 空格，>=0 = 被对应索引的物品占据。
/// </summary>
public class BagGrid
{
    public int Width { get; private set; }
    public int Height { get; private set; }

    /// <summary>占位图 [y * Width + x]，值为 ItemIndex</summary>
    private int[] _occupancy;

    /// <summary>已放置物品列表</summary>
    private List<PlacedItem> _items = new List<PlacedItem>();

    /// <summary>物品变化事件</summary>
    public event Action OnChanged;

    public IReadOnlyList<PlacedItem> Items => _items;

    public BagGrid(int width, int height)
    {
        Width = width;
        Height = height;
        _occupancy = new int[width * height];
        for (int i = 0; i < _occupancy.Length; i++)
            _occupancy[i] = -1;
    }

    /// <summary>获取指定格子的物品索引，-1 表示空</summary>
    public int GetCell(int x, int y) => _occupancy[y * Width + x];

    /// <summary>检查指定位置是否可以放置物品</summary>
    public bool CanPlace(ItemShape shape, int gridX, int gridY)
    {
        for (int sx = 0; sx < shape.Width; sx++)
        {
            for (int sy = 0; sy < shape.Height; sy++)
            {
                if (!shape.Cells[sx, sy]) continue;

                int gx = gridX + sx;
                int gy = gridY + sy;

                // 出界检查
                if (gx < 0 || gx >= Width || gy < 0 || gy >= Height)
                    return false;

                // 占位检查
                if (_occupancy[gy * Width + gx] >= 0)
                    return false;
            }
        }
        return true;
    }

    /// <summary>在指定位置放置物品。成功返回 PlacedItem，失败返回 null。</summary>
    public PlacedItem PlaceItem(ItemData itemData, int gridX, int gridY, int rotation = 0)
    {
        var placedItem = new PlacedItem(itemData, gridX, gridY, rotation);

        if (!CanPlace(placedItem.RotatedShape, gridX, gridY))
            return null;

        placedItem.ItemIndex = _items.Count;
        _items.Add(placedItem);

        // 标记占位
        FillShape(placedItem.RotatedShape, gridX, gridY, placedItem.ItemIndex);

        OnChanged?.Invoke();
        return placedItem;
    }

    /// <summary>移除指定物品</summary>
    public bool RemoveItem(PlacedItem item)
    {
        if (item == null) return false;
        int idx = _items.IndexOf(item);
        if (idx < 0) return false;

        // 清除占位
        ClearShape(item.RotatedShape, item.GridX, item.GridY, idx);

        _items.RemoveAt(idx);

        // 重新分配索引
        for (int i = 0; i < _items.Count; i++)
            _items[i].ItemIndex = i;

        OnChanged?.Invoke();
        return true;
    }

    /// <summary>旋转已放置的物品。成功返回 true。</summary>
    public bool RotateItem(PlacedItem item)
    {
        if (item == null) return false;

        var newRotation = (item.Rotation + 1) % 4;
        var newShape = item.ItemData.GetShape().Rotate(newRotation);

        // 清除旧的占位
        ClearShape(item.RotatedShape, item.GridX, item.GridY, item.ItemIndex);

        // 检查新位置是否可行
        if (!CanPlace(newShape, item.GridX, item.GridY))
        {
            // 恢复旧的占位
            FillShape(item.RotatedShape, item.GridX, item.GridY, item.ItemIndex);
            return false;
        }

        // 应用新旋转
        item.SetRotation(newRotation);
        FillShape(item.RotatedShape, item.GridX, item.GridY, item.ItemIndex);

        OnChanged?.Invoke();
        return true;
    }

    /// <summary>聚合所有物品的属性</summary>
    public CombatStats GetTotalStats()
    {
        var stats = CombatStats.Zero;
        foreach (var item in _items)
            stats += item.GetStats();
        return stats;
    }

    /// <summary>清空整个网格</summary>
    public void Clear()
    {
        _items.Clear();
        for (int i = 0; i < _occupancy.Length; i++)
            _occupancy[i] = -1;
        OnChanged?.Invoke();
    }

    // --- 内部辅助 ---

    private void FillShape(ItemShape shape, int gx, int gy, int itemIndex)
    {
        for (int sx = 0; sx < shape.Width; sx++)
            for (int sy = 0; sy < shape.Height; sy++)
                if (shape.Cells[sx, sy])
                    _occupancy[(gy + sy) * Width + (gx + sx)] = itemIndex;
    }

    private void ClearShape(ItemShape shape, int gx, int gy, int expectedIndex)
    {
        for (int sx = 0; sx < shape.Width; sx++)
            for (int sy = 0; sy < shape.Height; sy++)
                if (shape.Cells[sx, sy])
                {
                    int idx = (gy + sy) * Width + (gx + sx);
                    if (_occupancy[idx] == expectedIndex)
                        _occupancy[idx] = -1;
                }
    }

    /// <summary>调试：打印占位图</summary>
    public void DebugPrint()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== BagGrid {Width}x{Height} ({_items.Count} items) ===");
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int v = _occupancy[y * Width + x];
                sb.Append(v >= 0 ? v.ToString() : "·");
                sb.Append(' ');
            }
            sb.AppendLine();
        }
        Debug.Log(sb.ToString());
    }
}
