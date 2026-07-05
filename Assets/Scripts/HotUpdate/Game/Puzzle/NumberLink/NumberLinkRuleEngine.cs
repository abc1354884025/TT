using System.Collections.Generic;
using UnityEngine;

    /// <summary>数回规则引擎</summary>
    public class NumberLinkRuleEngine : IPuzzleRuleEngine
    {
        private readonly NumberLinkGrid _grid;

        public NumberLinkRuleEngine(NumberLinkGrid grid)
        {
            _grid = grid;
        }

        public bool IsSolutionValid()
        {
            return _grid.IsFullyCovered() && CheckNoIntersections() && CheckPathContinuity();
        }

        public bool IsMoveValid(PuzzleMove move)
        {
            // 操作合法性在 ViewModel 中实时处理
            return true;
        }

        public List<Vector2Int> GetViolations()
        {
            var violations = new List<Vector2Int>();

            // 检查路径交叉：每个非端点格子最多只有一条路径经过
            for (int x = 0; x < _grid.Grid.Width; x++)
            {
                for (int y = 0; y < _grid.Grid.Height; y++)
                {
                    var cell = _grid.Grid[x, y];
                    if (cell.IsEndpoint) continue;

                    int neighborCount = 0;
                    foreach (var n in _grid.Grid.GetNeighbors(x, y))
                    {
                        var nc = _grid.Grid[n];
                        if (nc.NumberValue == cell.NumberValue && nc.NumberValue > 0)
                            neighborCount++;
                    }

                    // 有路径经过但没有正确的出入口方向
                    if (cell.NumberValue > 0 && neighborCount < 1)
                        violations.Add(new Vector2Int(x, y));
                }
            }

            return violations;
        }

        private bool CheckNoIntersections()
        {
            for (int x = 0; x < _grid.Grid.Width; x++)
            {
                for (int y = 0; y < _grid.Grid.Height; y++)
                {
                    var cell = _grid.Grid[x, y];
                    // 检查是否有不同数字的路径重叠
                    if (cell.NumberValue > 0 && !cell.IsEndpoint)
                    {
                        // 确认四周的颜色一致性
                        int expectedValue = cell.NumberValue;
                        foreach (var n in _grid.Grid.GetNeighbors(x, y))
                        {
                            var nc = _grid.Grid[n];
                            if (nc.NumberValue > 0 && nc.NumberValue != expectedValue
                                && !nc.IsEndpoint)
                                return false; // 颜色交叉
                        }
                    }
                }
            }
            return true;
        }

        private bool CheckPathContinuity()
        {
            // 验证每条路径从端点开始、在端点结束，中间连续不断
            foreach (var kv in _grid.ActivePaths)
            {
                if (kv.Value.Count == 0) continue;
                // 基础连续性检查：每个路径格子必须有且仅有两个方向的连接（除了路径两端）
                foreach (var pos in kv.Value)
                {
                    var cell = _grid.Grid[pos];
                    if (!cell.IsEndpoint)
                    {
                        int dirs = (cell.InDir != PuzzleDirection.None ? 1 : 0)
                            + (cell.OutDir != PuzzleDirection.None ? 1 : 0);
                        if (dirs != 2) return false; // 路径中间格子必须有出入口
                    }
                }
            }
            return true;
        }
    }
