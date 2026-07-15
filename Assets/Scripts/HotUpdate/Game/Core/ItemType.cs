/// <summary>物品类型，影响可放置的背包位置和战斗加成类别</summary>
public enum ItemType
{
    Weapon,      // 武器：主要加攻击
    Armor,       // 防具：主要加防御/生命
    Trinket      // 饰品：特殊效果/暴击等
}

/// <summary>决定物品放入网格时是解锁格子，还是作为战斗装备占用格子。</summary>
public enum ItemPlacementType
{
    Equipment,
    BackpackItem
}
