using System;
using System.Collections.Generic;
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

    [Header("商店")]
    [SerializeField] private UIList _shopList;
    [SerializeField] private Button _refreshShopButton;

    [Header("物品栏")]
    [SerializeField] private UIList _inventoryList;

    [Header("操作")]
    [SerializeField] private Button _battleButton;
    [SerializeField] private Button _backButton;

    private PrepareViewModel _vm;
    private readonly List<Action> _unbind = new List<Action>();

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

        // 商店
        if (_shopList) RefreshShopUI();
        if (_refreshShopButton)
            _unbind.Add(_refreshShopButton.BindClick(OnRefreshShop));

        // 物品栏
        if (_inventoryList) RefreshInventoryUI();

        // 按钮
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
        _shopList.SetData(_vm.ShopItems, (go, item, idx) =>
        {
            var widget = go.GetComponent<ShopItemWidget>();
            if (widget)
            {
                widget.Init(item, _vm.Gold.Value >= item.BuyPrice);
                widget.OnBuyClicked += OnBuyItem;
            }
        });
    }

    private void RefreshInventoryUI()
    {
        if (_inventoryList == null || _vm == null) return;
        _inventoryList.SetData(_vm.Inventory, (go, item, idx) =>
        {
            var nameText = go.transform.Find("NameText")?.GetComponent<TMP_Text>();
            if (nameText) nameText.text = item.Name;

            var btn = go.GetComponent<Button>();
            if (btn)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnItemClick(item));
            }
        });
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
        // 点击物品 → 自动放置到第一个空位
        for (int y = 0; y < _vm.BagGrid.Height; y++)
        {
            for (int x = 0; x < _vm.BagGrid.Width; x++)
            {
                var shape = item.GetShape();
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
        // 关闭准备面板，打开战斗面板
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
