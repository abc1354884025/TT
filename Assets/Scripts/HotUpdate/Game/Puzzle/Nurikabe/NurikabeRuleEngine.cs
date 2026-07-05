using System.Collections.Generic;
using UnityEngine;

    /// <summary>数墙规则引擎</summary>
    public class NurikabeRuleEngine : IPuzzleRuleEngine
    {
        private readonly NurikabeGrid _grid;

        public NurikabeRuleEngine(NurikabeGrid grid)
        {
            _grid = grid;
        }

        public bool IsSolutionValid()
        {
            return CheckIslandSizes() && CheckWallConnectivity()
                && CheckNoTwoByTwoBlack() && CheckIslandSeparation();
        }

        public bool IsMoveValid(PuzzleMove move)
        {
            // 所有切换操作都是临时合法的，规则验证在 CheckSolution 中统一执行
            return true;
        }

        public List<Vector2Int> GetViolations()
        {
            var violations = new List<Vector2Int>();
            var g = _grid.Grid;
            int w = g.Width, h = g.Height;

            // 检查2×2黑色块
            for (int x = 0; x < w - 1; x++)
            {
                for (int y = 0; y < h - 1; y++)
                {
                    bool allBlack = g[x, y].State == NurikabeCellState.Black
                        && g[x + 1, y].State == NurikabeCellState.Black
                        && g[x, y + 1].State == NurikabeCellState.Black
                        && g[x + 1, y + 1].State == NurikabeCellState.Black;
                    if (allBlack)
                    {
                        violations.Add(new Vector2Int(x, y));
                        violations.Add(new Vector2Int(x + 1, y));
                        violations.Add(new Vector2Int(x, y + 1));
                        violations.Add(new Vector2Int(x + 1, y + 1));
                    }
                }
            }

            return violations;
        }

        /// <summary>检查每个数字岛屿的大小是否正确</summary>
        private bool CheckIslandSizes()
        {
            var g = _grid.Grid;
            int w = g.Width, h = g.Height;
            var visited = new bool[w, h];

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    var cell = g[x, y];
                    if (cell.State == NurikabeCellState.NumberedWhite && !visited[x, y])
                    {
                        // Flood-fill 计算此岛屿的白色格子数
                        int expectedSize = cell.NumberValue;
                        int actualSize = FloodFillWhite(x, y, visited, out _);
                        if (actualSize != expectedSize)
                            return false;
                    }
                }
            }
            return true;
        }

        /// <summary>检查所有黑色格子是否连通（墙壁）</summary>
        private bool CheckWallConnectivity()
        {
            var g = _grid.Grid;
            int w = g.Width, h = g.Height;
            var visited = new bool[w, h];

            // 找到第一个黑色格子开始 flood-fill
            Vector2Int? firstBlack = null;
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    if (g[x, y].State == NurikabeCellState.Black)
                    { firstBlack = new Vector2Int(x, y); break; }

            if (firstBlack == null) return true; // 没有黑格

            int blackCount = FloodFillBlack(firstBlack.Value.x, firstBlack.Value.y, visited);

            // 所有黑色格子都应该被访问到
            int totalBlack = 0;
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    if (g[x, y].State == NurikabeCellState.Black) totalBlack++;

            return blackCount == totalBlack;
        }

        /// <summary>检查是否有 2×2 的纯黑色区域</summary>
        private bool CheckNoTwoByTwoBlack()
        {
            var g = _grid.Grid;
            int w = g.Width, h = g.Height;

            for (int x = 0; x < w - 1; x++)
                for (int y = 0; y < h - 1; y++)
                    if (g[x, y].State == NurikabeCellState.Black
                        && g[x + 1, y].State == NurikabeCellState.Black
                        && g[x, y + 1].State == NurikabeCellState.Black
                        && g[x + 1, y + 1].State == NurikabeCellState.Black)
                        return false;
            return true;
        }

        /// <summary>检查白色孤岛是否彼此分离（不能相邻）</summary>
        private bool CheckIslandSeparation()
        {
            var g = _grid.Grid;
            int w = g.Width, h = g.Height;
            var visited = new bool[w, h];
            var islandIds = new Dictionary<int, int>(); // islandId -> number

            // 为白色区域 flood-fill 并关联岛屿
            int nextId = 0;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    var cell = g[x, y];
                    if ((cell.State == NurikabeCellState.White || cell.State == NurikabeCellState.NumberedWhite)
                        && !visited[x, y])
                    {
                        FloodFillWhite(x, y, visited, out int foundNumber);
                        if (foundNumber > 0 && islandIds.ContainsKey(foundNumber))
                            return false; // 同一个数字的岛屿被分开了
                        if (foundNumber > 0)
                            islandIds[foundNumber] = nextId++;
                    }
                }
            }
            return true;
        }

        private int FloodFillWhite(int x, int y, bool[,] visited, out int foundNumber)
        {
            var g = _grid.Grid;
            int w = g.Width, h = g.Height;
            int count = 0;
            foundNumber = 0;

            var stack = new Stack<Vector2Int>();
            stack.Push(new Vector2Int(x, y));

            while (stack.Count > 0)
            {
                var pos = stack.Pop();
                if (!g.InBounds(pos) || visited[pos.x, pos.y]) continue;

                var cell = g[pos];
                if (cell.State == NurikabeCellState.Black) continue;

                visited[pos.x, pos.y] = true;
                count++;

                if (cell.State == NurikabeCellState.NumberedWhite)
                    foundNumber = cell.NumberValue;

                foreach (var n in g.GetNeighbors(pos))
                    stack.Push(n);
            }
            return count;
        }

        private int FloodFillBlack(int x, int y, bool[,] visited)
        {
            var g = _grid.Grid;
            int w = g.Width, h = g.Height;
            int count = 0;

            var stack = new Stack<Vector2Int>();
            stack.Push(new Vector2Int(x, y));

            while (stack.Count > 0)
            {
                var pos = stack.Pop();
                if (!g.InBounds(pos) || visited[pos.x, pos.y]) continue;

                var cell = g[pos];
                if (cell.State != NurikabeCellState.Black) continue;

                visited[pos.x, pos.y] = true;
                count++;

                foreach (var n in g.GetNeighbors(pos))
                    stack.Push(n);
            }
            return count;
        }
    }
