using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 数独棋盘单元格。挂在 GridCellWidget.prefab 上。
/// Panel 只调 SetContext(vm) + SetIndex(i)，其余自己搞定。
/// </summary>
public class SudokuCellWidget : GridCellWidget
{
    private SudokuViewModel _vm;
    private int _x;
    private int _y;

    /// <summary>设置上下文 VM（LoopScrollView OnCellInit 时调用一次）</summary>
    public void SetContext(SudokuViewModel vm)
    {
        _vm = vm;
    }

    /// <summary>设置线性索引，自动换算为 (x,y) 并刷新显示</summary>
    public void SetIndex(int index)
    {
        _x = index % 9;
        _y = index / 9;
        Refresh();
    }

    /// <summary>从 VM 读取最新数据并更新 UI</summary>
    public void Refresh()
    {
        if (_vm == null) return;

        var cell = _vm.Grid.Grid[_x, _y];

        // 数字
        SetNumber(cell.Value > 0 ? cell.Value.ToString() : "");

        // 背景色
        Color bg = Color.white;
        if (cell.IsError)
            bg = new Color(1f, 0.85f, 0.85f);           // 浅红：错误
        else if (cell.IsClue)
            bg = new Color(0.92f, 0.92f, 0.92f);        // 浅灰：线索
        else if (_vm.SelectedCell == new Vector2Int(_x, _y))
            bg = new Color(0.85f, 0.90f, 1f);           // 浅蓝：选中
        SetBackgroundColor(bg);

        // 字体粗细
        if (NumberText)
            NumberText.fontStyle = cell.IsClue ? FontStyles.Bold : FontStyles.Normal;
    }

    private void Awake()
    {
        if (Button)
            Button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (_vm == null) return;
        _vm.SelectCell(_x, _y);
        // 通知 Panel 刷新整个棋盘（高亮会变）
        _vm.RaiseGridChanged();
    }
}
