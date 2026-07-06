/// <summary>
/// 已放置在网格中的物品实例。
/// </summary>
public class PlacedItem
{
    /// <summary>物品定义</summary>
    public ItemData ItemData { get; private set; }

    /// <summary>在网格中的位置（左上角锚点）</summary>
    public int GridX { get; set; }
    public int GridY { get; set; }

    /// <summary>当前旋转次数（0-3，每 1 = 顺时针 90°）</summary>
    public int Rotation { get; private set; }

    /// <summary>旋转后的实际形状（缓存）</summary>
    public ItemShape RotatedShape { get; private set; }

    /// <summary>在网格中的唯一索引（由 BagGrid 分配）</summary>
    public int ItemIndex { get; set; }

    public PlacedItem(ItemData itemData, int gridX, int gridY, int rotation = 0)
    {
        ItemData = itemData;
        GridX = gridX;
        GridY = gridY;
        ItemIndex = -1;
        SetRotation(rotation);
    }

    /// <summary>设置旋转并更新形状缓存</summary>
    public void SetRotation(int rot)
    {
        Rotation = ((rot % 4) + 4) % 4;
        RotatedShape = ItemData.GetShape().Rotate(Rotation);
    }

    /// <summary>获取物品的战斗力</summary>
    public CombatStats GetStats() => ItemData.GetStats();

    public override string ToString()
        => $"{ItemData.Name} @ ({GridX},{GridY}) R{Rotation}";
}
