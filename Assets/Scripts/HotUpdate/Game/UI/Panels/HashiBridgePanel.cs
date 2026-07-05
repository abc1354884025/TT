using UnityEngine;

    /// <summary>数桥游戏面板</summary>
    public class HashiBridgePanel : PuzzleGamePanel
    {
        private HashiBridgeViewModel _hashiVM;

        protected override PuzzleGameViewModel CreateViewModel()
        {
            _hashiVM = new HashiBridgeViewModel();
            if (LevelData is HashiBridgeLevelData data)
                _hashiVM.InitFromLevel(data);
            return _hashiVM;
        }

        protected override bool EnableDrag() => true; // 拖拽建桥模式

        protected override void OnClose()
        {
            _hashiVM = null;
            base.OnClose();
        }

        protected override void RenderGrid()
        {
            if (GridRenderer == null || _hashiVM == null) return;

            int w = _hashiVM.Grid.Grid.Width;
            int h = _hashiVM.Grid.Grid.Height;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var go = GridRenderer.GetCell(x, y, w);
                    if (!go) continue;

                    var widget = go.GetComponent<GridCellWidget>();
                    if (!widget) widget = go.AddComponent<GridCellWidget>();

                    var cell = _hashiVM.Grid.Grid[x, y];
                    var pos = new Vector2Int(x, y);

                    // 岛屿显示数字和桥数量
                    if (cell.Type == HashiCellType.Island)
                    {
                        widget.SetNumber($"{cell.CurrentBridgeCount}/{cell.IslandValue}");

                        // 完成则绿色，否则根据拖拽起始高亮
                        Color bgColor = cell.IsComplete
                            ? new Color(0.6f, 1f, 0.6f)           // 浅绿 = 完成
                            : new Color(1f, 0.95f, 0.7f);         // 暖色 = 岛屿

                        if (_hashiVM.DragStartIsland == pos)
                            bgColor = new Color(0.7f, 0.85f, 1f); // 浅蓝 = 选中

                        widget.SetBackgroundColor(bgColor);
                    }
                    else
                    {
                        // 海格不显示文字
                        widget.SetNumber("");
                        widget.SetBackgroundColor(new Color(0.85f, 0.92f, 1f)); // 浅蓝 = 海
                    }
                }
            }

            // TODO: 渲染桥线（需要额外的 LineRenderer 或 Image 系统，MVP 可以用颜色表示）
            // 在 MVP 中，可以通过在相邻岛屿之间的海格上着色来表示桥
            foreach (var kv in _hashiVM.Grid.Bridges)
            {
                var (a, b) = kv.Key;
                int count = kv.Value;
                if (count <= 0) continue;

                // 在两个岛之间的海格上标记
                var between = _hashiVM.Grid.Grid.GetCellsBetween(a, b);
                foreach (var bp in between)
                {
                    var bgo = GridRenderer.GetCell(bp.x, bp.y, w);
                    if (!bgo) continue;
                    var bw = bgo.GetComponent<GridCellWidget>();
                    if (!bw) bw = bgo.AddComponent<GridCellWidget>();

                    // 单桥=浅色, 双桥=深色
                    Color bridgeColor = count >= 2
                        ? new Color(0.3f, 0.45f, 0.3f)
                        : new Color(0.55f, 0.7f, 0.55f);
                    bw.SetBackgroundColor(bridgeColor);
                }
            }
        }
    }
