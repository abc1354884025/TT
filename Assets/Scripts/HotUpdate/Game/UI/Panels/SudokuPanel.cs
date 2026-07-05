using System;
using System.Collections.Generic;
using KingSoft.UI;
using UnityEngine;

/// <summary>
/// 数独游戏面板。使用 LoopScrollView 动态生成 9×9 棋盘。
/// cellNumOfColumn = 9 → 自动排列为 9列 × 9行。
/// </summary>
public class SudokuPanel : PuzzleGamePanel
{
    [Header("数独特有")]
    [SerializeField] private NumberButtonWidget[] _numberButtons;  // 0=erase, 1-9=digits

    private SudokuViewModel _sudokuVM;

    protected override bool UseLoopScrollGrid => true;
    protected override bool EnableDrag() => false;

    protected override PuzzleGameViewModel CreateViewModel()
    {
        _sudokuVM = new SudokuViewModel();
        if (LevelData is SudokuLevelData sudokuData)
            _sudokuVM.InitFromLevel(sudokuData);
        return _sudokuVM;
    }

    protected override void OnOpen(object data)
    {
        base.OnOpen(data);

        // 绑定数字按钮
        if (_numberButtons != null)
        {
            for (int i = 0; i < _numberButtons.Length; i++)
            {
                var btn = _numberButtons[i];
                if (btn == null) continue;
                btn.Init(i);
                btn.OnNumberClicked += OnNumberClicked;
            }
        }

        // 订阅 VM 网格变化 → 刷新 LoopScrollView 可见 cell
        if (_sudokuVM != null)
            _sudokuVM.OnGridChanged += RefreshVisibleCells;
    }

    protected override void OnClose()
    {
        if (_numberButtons != null)
            foreach (var btn in _numberButtons)
                if (btn) btn.OnNumberClicked -= OnNumberClicked;

        if (_sudokuVM != null)
            _sudokuVM.OnGridChanged -= RefreshVisibleCells;

        _sudokuVM = null;
        base.OnClose();
    }

    // === LoopScrollView 网格模式 ===

    /// <summary>LoopScrollView 每个 cell 进入可见区域时：设 VM + 设 index</summary>
    protected override void OnGridCellUpdate(int index, GameObject go)
    {
        var widget = go.GetComponent<SudokuCellWidget>();
        if (!widget) widget = go.AddComponent<SudokuCellWidget>();
        widget.SetContext(_sudokuVM);
        widget.SetIndex(index);
    }

    /// <summary>VM 数据变化时刷新当前可见的 cell（不重建整个棋盘）</summary>
    private void RefreshVisibleCells()
    {
        if (GridScrollView == null || _sudokuVM == null) return;

        var allCells = GridScrollView.GetAllCellObjects();
        foreach (var kv in allCells)
        {
            var widget = kv.Value.GetComponent<SudokuCellWidget>();
            if (widget) widget.Refresh();
        }
    }

    /// <summary>传统 RenderGrid：LoopScrollView 模式下用 ReloadData 触发 OnCellUpdate</summary>
    protected override void RenderGrid()
    {
        // LoopScrollView 模式下，ReloadData 会触发所有可见 cell 的 OnCellUpdate
        GridScrollView?.ReloadData(81, keepOffset: true);
    }

    // === 数字按钮 ===

    private void OnNumberClicked(int digit)
    {
        if (_sudokuVM == null) return;
        _sudokuVM.SetDigit(digit);
        RefreshVisibleCells();
        _sudokuVM.CheckSolution();
    }
}
