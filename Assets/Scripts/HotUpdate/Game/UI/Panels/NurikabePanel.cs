using UnityEngine;

    /// <summary>数墙游戏面板</summary>
    public class NurikabePanel : PuzzleGamePanel
    {
        private NurikabeViewModel _nurikabeVM;

        protected override PuzzleGameViewModel CreateViewModel()
        {
            _nurikabeVM = new NurikabeViewModel();
            if (LevelData is NurikabeLevelData data)
                _nurikabeVM.InitFromLevel(data);
            return _nurikabeVM;
        }

        protected override bool EnableDrag() => false; // 纯点击模式

        protected override void OnClose()
        {
            _nurikabeVM = null;
            base.OnClose();
        }

        protected override void RenderGrid()
        {
            if (GridRenderer == null || _nurikabeVM == null) return;

            int w = _nurikabeVM.Grid.Grid.Width;
            int h = _nurikabeVM.Grid.Grid.Height;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var go = GridRenderer.GetCell(x, y, w);
                    if (!go) continue;

                    var widget = go.GetComponent<GridCellWidget>();
                    if (!widget) widget = go.AddComponent<GridCellWidget>();

                    var cell = _nurikabeVM.Grid.Grid[x, y];

                    // 数字显示
                    if (cell.NumberValue > 0)
                        widget.SetNumber(cell.NumberValue.ToString());
                    else
                        widget.SetNumber("");

                    // 背景色
                    Color bgColor;
                    switch (cell.State)
                    {
                        case NurikabeCellState.Black:
                            bgColor = new Color(0.2f, 0.2f, 0.2f);  // 深色 = 墙壁
                            break;
                        case NurikabeCellState.NumberedWhite:
                            bgColor = new Color(1f, 0.95f, 0.75f); // 暖色 = 数字线索
                            break;
                        case NurikabeCellState.White:
                        default:
                            bgColor = Color.white;                    // 白色 = 岛屿
                            break;
                    }
                    widget.SetBackgroundColor(bgColor);

                    // 文字颜色（黑格白字）
                    if (widget.NumberText)
                        widget.NumberText.color = cell.State == NurikabeCellState.Black
                            ? Color.white : Color.black;
                }
            }
        }
    }
