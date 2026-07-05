using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

    /// <summary>
    /// 数独游戏面板。
    /// </summary>
    public class SudokuPanel : PuzzleGamePanel
    {
        [Header("数独特有")]
        [SerializeField] private NumberButtonWidget[] _numberButtons;  // 0=erase, 1-9=digits

        private SudokuViewModel _sudokuVM;

        protected override PuzzleGameViewModel CreateViewModel()
        {
            _sudokuVM = new SudokuViewModel();
            if (LevelData is SudokuLevelData sudokuData)
                _sudokuVM.InitFromLevel(sudokuData);
            return _sudokuVM;
        }

        protected override bool EnableDrag() => false; // 数独用点击模式

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
                    int digit = i; // 0 = erase, 1-9 = digits
                    btn.Init(digit);
                    btn.OnNumberClicked += OnNumberClicked;
                }
            }
        }

        protected override void OnClose()
        {
            // 清理数字按钮事件
            if (_numberButtons != null)
                foreach (var btn in _numberButtons)
                    if (btn) btn.OnNumberClicked -= OnNumberClicked;

            _sudokuVM = null;
            base.OnClose();
        }

        protected override void RenderGrid()
        {
            if (GridRenderer == null || _sudokuVM == null) return;

            int w = 9, h = 9;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var go = GridRenderer.GetCell(x, y, w);
                    if (!go) continue;

                    var widget = go.GetComponent<GridCellWidget>();
                    if (!widget) widget = go.AddComponent<GridCellWidget>();

                    var cell = _sudokuVM.Grid.Grid[x, y];

                    // 数字显示
                    widget.SetNumber(cell.Value > 0 ? cell.Value.ToString() : "");

                    // 背景色
                    Color bgColor = Color.white;
                    if (cell.IsError)
                        bgColor = new Color(1f, 0.85f, 0.85f);  // 浅红（错误）
                    else if (cell.IsClue)
                        bgColor = new Color(0.92f, 0.92f, 0.92f); // 浅灰（线索）
                    else if (_sudokuVM.SelectedCell == new Vector2Int(x, y))
                        bgColor = new Color(0.85f, 0.90f, 1f);   // 浅蓝（选中）

                    widget.SetBackgroundColor(bgColor);

                    // 字体粗细（线索加粗）
                    if (widget.NumberText)
                        widget.NumberText.fontStyle = cell.IsClue ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
                }
            }
        }

        private void OnNumberClicked(int digit)
        {
            if (_sudokuVM == null) return;
            _sudokuVM.SetDigit(digit);
            RenderGrid();
            _sudokuVM.CheckSolution();
        }
    }
