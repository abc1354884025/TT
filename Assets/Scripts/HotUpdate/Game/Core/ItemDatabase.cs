using System.Collections.Generic;

/// <summary>
/// 物品数据库——MVP 版本写死数据，后续热更从 JSON 加载覆盖。
/// </summary>
public static class ItemDatabase
{
    private static Dictionary<string, ItemData> _items;
    private static List<ItemData> _itemList;

    public static bool IsLoaded => _items != null;
    public static int Count => _itemList?.Count ?? 0;
    public static IReadOnlyList<ItemData> AllItems => _itemList;

    static ItemDatabase()
    {
        LoadBuiltinItems();
    }

    /// <summary>硬编码物品</summary>
    private static void LoadBuiltinItems()
    {
        _items = new Dictionary<string, ItemData>();
        _itemList = new List<ItemData>();

        Add(new ItemData { Id = "rusty_sword",  Name = "铁剑",   Rarity = ItemRarity.Common,    Type = ItemType.Weapon,  ShapeMatrix = new[] { new[] { 1 }, new[] { 1 }, new[] { 1 } },                                           Attack = 5,  Defense = 0,  HP = 0,  CritChance = 5,  SellPrice = 5,  BuyPrice = 15, Description = "普通铁剑，攻击+5" });
        Add(new ItemData { Id = "wooden_shield", Name = "木盾",  Rarity = ItemRarity.Common,    Type = ItemType.Armor,   ShapeMatrix = new[] { new[] { 1, 1 }, new[] { 1, 1 } },                                                     Attack = 0,  Defense = 4,  HP = 5,  CritChance = 0,  SellPrice = 5,  BuyPrice = 15, Description = "木质圆盾，防御+4，生命+5" });
        Add(new ItemData { Id = "dagger",       Name = "匕首",  Rarity = ItemRarity.Common,    Type = ItemType.Weapon,  ShapeMatrix = new[] { new[] { 1, 0 }, new[] { 0, 1 } },                                                     Attack = 6,  Defense = 0,  HP = 0,  CritChance = 15, SellPrice = 8,  BuyPrice = 20, Description = "短小精悍，暴击+15" });
        Add(new ItemData { Id = "broadsword",   Name = "长剑",  Rarity = ItemRarity.Uncommon,  Type = ItemType.Weapon,  ShapeMatrix = new[] { new[] { 1, 1, 1, 0 }, new[] { 0, 0, 0, 1 } },                                         Attack = 10, Defense = 0,  HP = 0,  CritChance = 10, SellPrice = 15, BuyPrice = 35, Description = "L 形长剑，攻击+10" });
        Add(new ItemData { Id = "viking_helmet",Name = "维京头盔", Rarity = ItemRarity.Uncommon,Type = ItemType.Armor,   ShapeMatrix = new[] { new[] { 1, 1, 1 }, new[] { 0, 1, 0 } },                                                   Attack = 0,  Defense = 6,  HP = 10, CritChance = 0,  SellPrice = 12, BuyPrice = 30, Description = "T 形头盔，防御+6 生命+10" });
        Add(new ItemData { Id = "health_potion",Name = "生命药水",Rarity = ItemRarity.Common,    Type = ItemType.Trinket, ShapeMatrix = new[] { new[] { 1 } },                                                                   Attack = 0,  Defense = 0,  HP = 10, CritChance = 0,  SellPrice = 3,  BuyPrice = 10, Description = "恢复生命，生命+10" });
        Add(new ItemData { Id = "battle_axe",   Name = "战斧",  Rarity = ItemRarity.Rare,      Type = ItemType.Weapon,  ShapeMatrix = new[] { new[] { 1, 1 }, new[] { 1, 0 }, new[] { 1, 0 } },                                         Attack = 15, Defense = 0,  HP = 0,  CritChance = 10, SellPrice = 25, BuyPrice = 55, Description = "重型战斧，攻击+15" });
        Add(new ItemData { Id = "plate_armor",  Name = "板甲",  Rarity = ItemRarity.Rare,      Type = ItemType.Armor,   ShapeMatrix = new[] { new[] { 1, 1, 1 }, new[] { 1, 0, 1 } },                                                   Attack = 0,  Defense = 10, HP = 15, CritChance = 0,  SellPrice = 20, BuyPrice = 50, Description = "厚重板甲，防御+10 生命+15" });
        Add(new ItemData { Id = "lucky_charm",  Name = "幸运符",Rarity = ItemRarity.Uncommon,  Type = ItemType.Trinket, ShapeMatrix = new[] { new[] { 0, 1, 0 }, new[] { 1, 0, 1 } },                                             Attack = 2,  Defense = 2,  HP = 3,  CritChance = 20, SellPrice = 10, BuyPrice = 28, Description = "十字幸运符，暴击+20" });
        Add(new ItemData { Id = "dragon_slayer",Name = "屠龙剑",Rarity = ItemRarity.Legendary, Type = ItemType.Weapon,  ShapeMatrix = new[] { new[] { 1, 1, 1, 1 }, new[] { 0, 0, 1, 0 } },                                             Attack = 30, Defense = 5,  HP = 5,  CritChance = 25, SellPrice = 60, BuyPrice = 120,Description = "传说之剑，攻击+30 暴击+25" });
        Add(new ItemData { Id = "spear",        Name = "长矛",  Rarity = ItemRarity.Uncommon,  Type = ItemType.Weapon,  ShapeMatrix = new[] { new[] { 1 }, new[] { 1 }, new[] { 0 }, new[] { 1 } },                                     Attack = 8,  Defense = 0,  HP = 0,  CritChance = 5,  SellPrice = 10, BuyPrice = 25, Description = "双手长矛，攻击+8" });
        Add(new ItemData { Id = "magic_ring",   Name = "魔法戒指",Rarity = ItemRarity.Epic,     Type = ItemType.Trinket, ShapeMatrix = new[] { new[] { 1, 1 }, new[] { 1, 1 } },                                                     Attack = 5,  Defense = 5,  HP = 10, CritChance = 10, SellPrice = 35, BuyPrice = 80, Description = "蕴含魔力，全属性加成" });

        UnityEngine.Debug.Log($"[ItemDatabase] 加载了 {_itemList.Count} 个物品（硬编码）");
    }

    private static void Add(ItemData item)
    {
        _items[item.Id] = item;
        _itemList.Add(item);
    }

    public static ItemData GetById(string id)
    {
        _items.TryGetValue(id, out var item);
        return item;
    }

    public static ItemData GetByIndex(int index)
    {
        return (index >= 0 && index < _itemList.Count) ? _itemList[index] : null;
    }

    public static List<ItemData> GetByRarity(ItemRarity rarity)
    {
        var result = new List<ItemData>();
        foreach (var item in _itemList)
            if (item.Rarity == rarity) result.Add(item);
        return result;
    }

    public static ItemData GetRandom(System.Random rng = null)
    {
        if (_itemList.Count == 0) return null;
        rng ??= new System.Random();
        return _itemList[rng.Next(_itemList.Count)];
    }
}
