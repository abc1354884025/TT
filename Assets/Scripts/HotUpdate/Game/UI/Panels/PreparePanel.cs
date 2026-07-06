using System;
using System.Collections.Generic;
using KingSoft.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 准备面板——背包乱斗的核心界面。
/// 顶部属性栏 + 左网格 + 右商店 + 底部物品栏 + 战斗按钮。
/// </summary>
public class PreparePanel : UIPanel
{
    [Header("属性栏")]
    [SerializeField] private StatBarWidget _atkBar;
    [SerializeField] private StatBarWidget _defBar;
    [SerializeField] private StatBarWidget _hpBar;
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private TMP_Text _roundText;

    [Header("背包网格")]
    [SerializeField] private BackpackGridWidget _gridWidget;

    [Header("商店列表（LoopScrollView）")]
    [SerializeField] private LoopScrollView _shopList;

    [Header("物品栏列表（LoopScrollView）")]
    [SerializeField] private LoopScrollView _inventoryList;

    [Header("操作")]
    [SerializeField] private Button _refreshShopButton;
    [SerializeField] private Button _battleButton;
    [SerializeField] private Button _backButton;

    private PrepareViewModel _vm;
    private readonly List<Action> _unbind = new List<Action>();

    private List<ItemData> _lastShopItems;
    private List<ItemData> _lastInventoryItems;

    #region 生命周期

    protected override void OnOpen(object data)
    {
        _vm = new PrepareViewModel();

        // 绑定属性栏
        if (_atkBar) _atkBar.Init("攻击");
        if (_defBar) _defBar.Init("防御");
        if (_hpBar) _hpBar.Init("生命");

        _unbind.Add(_goldText.BindTo(_vm.Gold));
        _unbind.Add(_roundText.BindTo(_vm.Round));

        // 背包网格
        if (_gridWidget) _gridWidget.Bind(_vm.BagGrid);

        // 属性面板实时更新
        _vm.CurrentStats.OnChanged += OnStatsChanged;
        _vm.CurrentStats.Refresh();

        // 商店 LoopScrollView
        if (_shopList)
        {
            _shopList.OnCellInit.AddListener(OnShopCellInit);
            _shopList.OnCellUpdate.AddListener(OnShopCellUpdate);
        }
        RefreshShopUI();

        // 物品栏 LoopScrollView
        if (_inventoryList)
        {
            _inventoryList.OnCellInit.AddListener(OnInventoryCellInit);
            _inventoryList.OnCellUpdate.AddListener(OnInventoryCellUpdate);
        }
        RefreshInventoryUI();

        // 按钮
        if (_refreshShopButton)
            _unbind.Add(_refreshShopButton.BindClick(OnRefreshShop));
        if (_battleButton)
            _unbind.Add(_battleButton.BindClick(OnStartBattle));
        if (_backButton)
            _unbind.Add(_backButton.BindClick(OnBack));
    }

    protected override void OnClose()
    {
        foreach (var u in _unbind) u.Invoke();
        _unbind.Clear();

        _vm.CurrentStats.OnChanged -= OnStatsChanged;
        _vm?.Dispose();
        _vm = null;

        // 清理事件
        if (_shopList)
        {
            _shopList.OnCellInit.RemoveAllListeners();
            _shopList.OnCellUpdate.RemoveAllListeners();
        }
        if (_inventoryList)
        {
            _inventoryList.OnCellInit.RemoveAllListeners();
            _inventoryList.OnCellUpdate.RemoveAllListeners();
        }
    }

    #endregion

    #region 商店

    private void OnShopCellInit(GameObject cell)
    {
        // 首次创建 cell 时挂 ShopItemWidget
        if (!cell.GetComponent<ShopItemWidget>())
            cell.AddComponent<ShopItemWidget>();
    }

    private void OnShopCellUpdate(int index, GameObject cell)
    {
        if (_lastShopItems == null || index >= _lastShopItems.Count) return;
        var item = _lastShopItems[index];
        var widget = cell.GetComponent<ShopItemWidget>();
        if (widget)
        {
            widget.Init(item, _vm.Gold.Value >= item.BuyPrice);
            widget.OnBuyClicked -= OnBuyItem;
            widget.OnBuyClicked += OnBuyItem;
        }
    }

    #endregion

    #region 物品栏

    private void OnInventoryCellInit(GameObject cell)
    {
        if (!cell.GetComponent<InventoryItemWidget>())
        {
            var widget = cell.AddComponent<InventoryItemWidget>();
            widget.OnClicked += OnItemClick;
        }
    }

    private void OnInventoryCellUpdate(int index, GameObject cell)
    {
        if (_lastInventoryItems == null || index >= _lastInventoryItems.Count) return;
        var item = _lastInventoryItems[index];
        var widget = cell.GetComponent<InventoryItemWidget>();
        if (widget) widget.Init(item);
    }

    #endregion

    #region UI 刷新

    private void OnStatsChanged(CombatStats stats)
    {
        if (_atkBar) _atkBar.SetValue(stats.Attack);
        if (_defBar) _defBar.SetValue(stats.Defense);
        if (_hpBar) _hpBar.SetValue(stats.MaxHP);
    }

    private void RefreshShopUI()
    {
        if (_shopList == null || _vm == null) return;
        _lastShopItems = _vm.ShopItems;
        _shopList.Initialize(null, _lastShopItems.Count);
        _shopList.ReloadData(_lastShopItems.Count);
    }

    private void RefreshInventoryUI()
    {
        if (_inventoryList == null || _vm == null) return;
        _lastInventoryItems = _vm.Inventory;
        _inventoryList.Initialize(null, _lastInventoryItems.Count);
        _inventoryList.ReloadData(_lastInventoryItems.Count);
    }

    #endregion

    #region 事件处理

    private void OnBuyItem(ItemData item)
    {
        if (_vm.TryBuyItem(item))
        {
            RefreshShopUI();
            RefreshInventoryUI();
        }
    }

    private void OnRefreshShop()
    {
        _vm.RefreshShop();
        RefreshShopUI();
    }

    private void OnItemClick(ItemData item)
    {
        for (int y = 0; y < _vm.BagGrid.Height; y++)
        {
            for (int x = 0; x < _vm.BagGrid.Width; x++)
            {
                if (_vm.TryPlaceItem(item, x, y))
                {
                    RefreshInventoryUI();
                    return;
                }
            }
        }
        Debug.LogWarning($"[PreparePanel] 无法放置 {item.Name}，背包已满？");
    }

    private void OnStartBattle()
    {
        UIManager.Instance.Close(this);
        UIManager.Instance.Open("BattlePanel", _vm);
    }

    private void OnBack()
    {
        UIManager.Instance.Close(this);
        UIManager.Instance.Open("MainMenuPanel");
    }

    #endregion
}
