using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 准备阶段 ViewModel——核心游戏状态。
/// </summary>
public class PrepareViewModel : ObservableObject
{
    private System.Random _rng = new System.Random();

    // === 背包网格 ===
    public BagGrid BagGrid { get; private set; }

    // === 反应式属性 ===
    public BindableProperty<CombatStats> CurrentStats = new BindableProperty<CombatStats>(CombatStats.Zero);
    public BindableProperty<int> Gold = new BindableProperty<int>(100);
    public BindableProperty<int> Round = new BindableProperty<int>(1);

    // === 物品栏（未放入背包的物品） ===
    public List<ItemData> Inventory = new List<ItemData>();

    // === 商店 ===
    public List<ItemData> ShopItems = new List<ItemData>();
    private int _shopSlots = 5;

    public PrepareViewModel()
    {
        BagGrid = new BagGrid(ConfigLoader.GridWidth, ConfigLoader.GridHeight);
        BagGrid.OnChanged += OnGridChanged;

        var balance = ConfigLoader.Balance;
        if (balance != null)
        {
            Gold.Value = balance.startingGold;
            _shopSlots = balance.shopSlotCount;
        }

        ForwardProperty(nameof(CurrentStats), CurrentStats);
        ForwardProperty(nameof(Gold), Gold);
        ForwardProperty(nameof(Round), Round);

        // 给 3 个初始物品
        AddToInventory(ItemDatabase.GetByIndex(0)); // 铁剑
        AddToInventory(ItemDatabase.GetByIndex(1)); // 木盾
        AddToInventory(ItemDatabase.GetByIndex(2)); // 匕首

        RefreshShop();
    }

    // === 背包操作 ===

    public bool TryPlaceItem(ItemData data, int gx, int gy, int rotation = 0)
    {
        var placed = BagGrid.PlaceItem(data, gx, gy, rotation);
        if (placed != null)
        {
            Inventory.Remove(data);
            return true;
        }
        return false;
    }

    public void RemoveFromGrid(PlacedItem item)
    {
        if (BagGrid.RemoveItem(item))
        {
            AddToInventory(item.ItemData);
        }
    }

    public bool TryRotateItem(PlacedItem item)
    {
        return BagGrid.RotateItem(item);
    }

    // === 商店 ===

    public bool TryBuyItem(ItemData data)
    {
        if (Gold.Value < data.BuyPrice) return false;
        Gold.Value -= data.BuyPrice;
        AddToInventory(data);
        ShopItems.Remove(data);
        return true;
    }

    public void RefreshShop()
    {
        ShopItems.Clear();
        var allItems = ItemDatabase.AllItems;
        if (allItems == null || allItems.Count == 0) return;

        for (int i = 0; i < _shopSlots; i++)
        {
            ShopItems.Add(allItems[_rng.Next(allItems.Count)]);
        }
    }

    // === 内部 ===

    private void AddToInventory(ItemData data)
    {
        if (data != null) Inventory.Add(data);
    }

    private void OnGridChanged()
    {
        CurrentStats.Value = BagGrid.GetTotalStats();
    }

    public override void Dispose()
    {
        BagGrid.OnChanged -= OnGridChanged;
        CurrentStats.Clear();
        Gold.Clear();
        Round.Clear();
        base.Dispose();
    }
}
