using System;

/// <summary>
/// 物品定义——纯数据 POCO。从 JSON 反序列化，支持 CDN 热更。
/// </summary>
[Serializable]
public class ItemData
{
    /// <summary>唯一 ID，如 "rusty_sword"</summary>
    public string Id;

    /// <summary>显示名称</summary>
    public string Name;

    /// <summary>图标路径（Resources 或 AB 中的 Sprite 路径）</summary>
    public string IconPath;

    /// <summary>稀有度</summary>
    public ItemRarity Rarity;

    /// <summary>类型（武器/防具/饰品）</summary>
    public ItemType Type;

    /// <summary>形状定义（JSON int[][] 反序列化）</summary>
    public int[][] ShapeMatrix;

    /// <summary>战斗属性加成</summary>
    public int Attack;
    public int Defense;
    public int HP;
    public int CritChance;

    /// <summary>描述文本</summary>
    public string Description;

    /// <summary>效果列表（JSON 字符串数组，如 [{"trigger":"Periodic","interval":5,"action":"heal","value":1}]）</summary>
    public string Effects;

    /// <summary>售价（金币）</summary>
    public int SellPrice;

    /// <summary>购买价</summary>
    public int BuyPrice;

    /// <summary>从 C# 获取解析后的形状</summary>
    public ItemShape GetShape() => ItemShape.FromJson(ShapeMatrix);

    /// <summary>获取属性结构体</summary>
    public CombatStats GetStats()
    {
        return new CombatStats
        {
            Attack = Attack,
            Defense = Defense,
            MaxHP = HP,
            CritChance = CritChance,
            AttackSpeed = 1f
        };
    }

    public override string ToString() => $"{Name} ({Rarity})";
}
