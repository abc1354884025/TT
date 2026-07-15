using System;
using System.Collections.Generic;
using UnityEngine;

public enum GridCellState
{
    Disabled,
    Idle,
    Occupied
}

/// <summary>背包网格：背包物品解锁禁用格，装备占用闲置格。</summary>
public class BagGrid
{
    public int Width { get; private set; }
    public int Height { get; private set; }

    private int[] _occupancy;
    private GridCellState[] _cellStates;
    private List<PlacedItem> _items = new List<PlacedItem>();

    public event Action OnChanged;
    public IReadOnlyList<PlacedItem> Items => _items;

    public BagGrid(int width, int height)
    {
        Width = width;
        Height = height;
        _occupancy = new int[width * height];
        _cellStates = new GridCellState[width * height];
        for (var i = 0; i < _occupancy.Length; i++)
        {
            _occupancy[i] = -1;
            _cellStates[i] = GridCellState.Disabled;
        }
    }

    public int GetCell(int x, int y) => _occupancy[y * Width + x];
    public GridCellState GetCellState(int x, int y) => _cellStates[y * Width + x];

    /// <summary>根据物品用途校验目标区域的统一状态。</summary>
    public bool CanPlace(ItemData itemData, ItemShape shape, int gridX, int gridY)
    {
        if (itemData == null || shape == null) return false;
        var requiredState = itemData.PlacementType == ItemPlacementType.BackpackItem
            ? GridCellState.Disabled
            : GridCellState.Idle;

        for (var sx = 0; sx < shape.Width; sx++)
        {
            for (var sy = 0; sy < shape.Height; sy++)
            {
                if (!shape.Cells[sx, sy]) continue;
                var gx = gridX + sx;
                var gy = gridY + sy;
                if (gx < 0 || gx >= Width || gy < 0 || gy >= Height) return false;
                if (GetCellState(gx, gy) != requiredState) return false;
            }
        }
        return true;
    }

    /// <summary>兼容旧调用：默认按装备校验。</summary>
    public bool CanPlace(ItemShape shape, int gridX, int gridY)
    {
        return CanPlace(new ItemData { PlacementType = ItemPlacementType.Equipment }, shape, gridX, gridY);
    }

    /// <summary>
    /// 放置背包物品时仅改变格子状态并消耗该物品；放置装备时创建可拖动的 PlacedItem。
    /// 成功均返回非空对象，供现有拖拽流程识别成功。
    /// </summary>
    public PlacedItem PlaceItem(ItemData itemData, int gridX, int gridY, int rotation = 0)
    {
        if (itemData == null) return null;
        var placedItem = new PlacedItem(itemData, gridX, gridY, rotation);
        if (!CanPlace(itemData, placedItem.RotatedShape, gridX, gridY)) return null;

        if (itemData.PlacementType == ItemPlacementType.BackpackItem)
        {
            SetShapeState(placedItem.RotatedShape, gridX, gridY, GridCellState.Idle);
            OnChanged?.Invoke();
            return placedItem;
        }

        placedItem.ItemIndex = _items.Count;
        _items.Add(placedItem);
        FillShape(placedItem.RotatedShape, gridX, gridY, placedItem.ItemIndex);
        OnChanged?.Invoke();
        return placedItem;
    }

    public bool RemoveItem(PlacedItem item)
    {
        if (item == null) return false;
        var index = _items.IndexOf(item);
        if (index < 0) return false;

        ClearShape(item.RotatedShape, item.GridX, item.GridY, index);
        _items.RemoveAt(index);
        for (var i = 0; i < _items.Count; i++) _items[i].ItemIndex = i;
        OnChanged?.Invoke();
        return true;
    }

    public bool RotateItem(PlacedItem item)
    {
        if (item == null) return false;
        var newRotation = (item.Rotation + 1) % 4;
        var newShape = item.ItemData.GetShape().Rotate(newRotation);
        ClearShape(item.RotatedShape, item.GridX, item.GridY, item.ItemIndex);
        if (!CanPlace(item.ItemData, newShape, item.GridX, item.GridY))
        {
            FillShape(item.RotatedShape, item.GridX, item.GridY, item.ItemIndex);
            return false;
        }

        item.SetRotation(newRotation);
        FillShape(item.RotatedShape, item.GridX, item.GridY, item.ItemIndex);
        OnChanged?.Invoke();
        return true;
    }

    public CombatStats GetTotalStats()
    {
        var stats = CombatStats.Zero;
        foreach (var item in _items) stats += item.GetStats();
        return stats;
    }

    public void Clear()
    {
        _items.Clear();
        for (var i = 0; i < _occupancy.Length; i++)
        {
            _occupancy[i] = -1;
            _cellStates[i] = GridCellState.Disabled;
        }
        OnChanged?.Invoke();
    }

    private void FillShape(ItemShape shape, int gx, int gy, int itemIndex)
    {
        for (var sx = 0; sx < shape.Width; sx++)
            for (var sy = 0; sy < shape.Height; sy++)
                if (shape.Cells[sx, sy])
                {
                    var index = (gy + sy) * Width + gx + sx;
                    _occupancy[index] = itemIndex;
                    _cellStates[index] = GridCellState.Occupied;
                }
    }

    private void ClearShape(ItemShape shape, int gx, int gy, int expectedIndex)
    {
        for (var sx = 0; sx < shape.Width; sx++)
            for (var sy = 0; sy < shape.Height; sy++)
                if (shape.Cells[sx, sy])
                {
                    var index = (gy + sy) * Width + gx + sx;
                    if (_occupancy[index] != expectedIndex) continue;
                    _occupancy[index] = -1;
                    _cellStates[index] = GridCellState.Idle;
                }
    }

    private void SetShapeState(ItemShape shape, int gx, int gy, GridCellState state)
    {
        for (var sx = 0; sx < shape.Width; sx++)
            for (var sy = 0; sy < shape.Height; sy++)
                if (shape.Cells[sx, sy]) _cellStates[(gy + sy) * Width + gx + sx] = state;
    }
}
