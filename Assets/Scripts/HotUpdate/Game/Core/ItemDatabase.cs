using System;
using System.Collections.Generic;
using TTSDK.UNBridgeLib.LitJson;

/// <summary>
/// 物品数据库。从 JSON 加载所有物品定义。
/// </summary>
public static class ItemDatabase
{
    private static Dictionary<string, ItemData> _items;
    private static List<ItemData> _itemList;

    /// <summary>是否已加载</summary>
    public static bool IsLoaded => _items != null;

    /// <summary>物品总数</summary>
    public static int Count => _itemList?.Count ?? 0;

    /// <summary>从 JSON 文本加载物品（可在运行时热更调用）</summary>
    public static void LoadFromJson(string json)
    {
        var wrapper = JsonMapper.ToObject<ItemListWrapper>(json);
        _items = new Dictionary<string, ItemData>();
        _itemList = new List<ItemData>();

        if (wrapper?.items != null)
        {
            foreach (var item in wrapper.items)
            {
                _items[item.Id] = item;
                _itemList.Add(item);
            }
        }

        UnityEngine.Debug.Log($"[ItemDatabase] 加载了 {_itemList.Count} 个物品");
    }

    /// <summary>按 ID 查找</summary>
    public static ItemData GetById(string id)
    {
        if (_items == null) return null;
        _items.TryGetValue(id, out var item);
        return item;
    }

    /// <summary>按索引查找</summary>
    public static ItemData GetByIndex(int index)
    {
        if (_itemList == null || index < 0 || index >= _itemList.Count)
            return null;
        return _itemList[index];
    }

    /// <summary>获取所有物品的只读列表</summary>
    public static IReadOnlyList<ItemData> AllItems => _itemList;

    /// <summary>按稀有度筛选</summary>
    public static List<ItemData> GetByRarity(ItemRarity rarity)
    {
        var result = new List<ItemData>();
        if (_itemList == null) return result;
        foreach (var item in _itemList)
            if (item.Rarity == rarity)
                result.Add(item);
        return result;
    }

    /// <summary>随机获取物品，可按稀有度加权</summary>
    public static ItemData GetRandom(System.Random rng = null)
    {
        if (_itemList == null || _itemList.Count == 0) return null;
        rng ??= new System.Random();
        return _itemList[rng.Next(_itemList.Count)];
    }

    [Serializable]
    private class ItemListWrapper
    {
        public ItemData[] items;
    }
}
