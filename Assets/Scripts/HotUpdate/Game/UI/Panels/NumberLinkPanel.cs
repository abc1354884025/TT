using UnityEngine;

    /// <summary>数回游戏面板</summary>
    public class NumberLinkPanel : PuzzleGamePanel
    {
        private NumberLinkViewModel _numLinkVM;

        protected override PuzzleGameViewModel CreateViewModel()
        {
            _numLinkVM = new NumberLinkViewModel();
            if (LevelData is NumberLinkLevelData data)
                _numLinkVM.InitFromLevel(data);
            return _numLinkVM;
        }

        protected override bool EnableDrag() => true; // 拖拽画线模式

        protected override void OnClose()
        {
            _numLinkVM = null;
            base.OnClose();
        }

        protected override void RenderGrid()
        {
            if (GridRenderer == null || _numLinkVM == null) return;

            int w = _numLinkVM.Grid.Grid.Width;
            int h = _numLinkVM.Grid.Grid.Height;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var go = GridRenderer.GetCell(x, y, w);
                    if (!go) continue;

                    var widget = go.GetComponent<GridCellWidget>();
                    if (!widget) widget = go.AddComponent<GridCellWidget>();

                    var cell = _numLinkVM.Grid.Grid[x, y];
                    var pos = new Vector2Int(x, y);

                    // 数字显示（仅端点显示数字）
                    if (cell.IsEndpoint)
                        widget.SetNumber(cell.NumberValue.ToString());
                    else
                        widget.SetNumber("");

                    // 背景色（路径用颜色区分）
                    Color bgColor = Color.white;
                    if (cell.NumberValue > 0)
                    {
                        int colorIdx = (cell.NumberValue - 1) % NumberLinkGrid.PathColors.Length;
                        bgColor = NumberLinkGrid.PathColors[colorIdx];
                        // 端点更深一些
                        if (cell.IsEndpoint)
                            bgColor = new Color(bgColor.r * 0.8f, bgColor.g * 0.8f, bgColor.b * 0.8f);
                        else
                            bgColor = new Color(bgColor.r * 0.6f, bgColor.g * 0.6f, bgColor.b * 0.6f, 0.6f);
                    }
                    widget.SetBackgroundColor(bgColor);

                    // 端点的数字颜色
                    if (widget.NumberText && cell.IsEndpoint)
                        widget.NumberText.color = Color.white;
                }
            }
        }
    }
