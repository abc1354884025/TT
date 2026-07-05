using System.Collections.Generic;
using UnityEngine;

    /// <summary>数独规则引擎：检查行/列/3×3宫的重复</summary>
    public class SudokuRuleEngine : IPuzzleRuleEngine
    {
        private readonly SudokuGrid _grid;

        public SudokuRuleEngine(SudokuGrid grid)
        {
            _grid = grid;
        }

        public bool IsSolutionValid()
        {
            return _grid.IsFilled() && GetViolations().Count == 0;
        }

        public bool IsMoveValid(PuzzleMove move)
        {
            if (move is SudokuMove sudokuMove)
            {
                int x = sudokuMove.Position.x;
                int y = sudokuMove.Position.y;
                int digit = sudokuMove.Digit;
                if (digit == 0) return true; // 擦除总是合法
                var cell = _grid.Grid[x, y];
                if (cell.IsClue) return false;
                return !HasConflict(x, y, digit);
            }
            return true;
        }

        public List<Vector2Int> GetViolations()
        {
            var violations = new List<Vector2Int>();
            var g = _grid.Grid;

            // 检查每行
            for (int y = 0; y < 9; y++)
            {
                var seen = new HashSet<int>();
                for (int x = 0; x < 9; x++)
                {
                    int v = g[x, y].Value;
                    if (v > 0 && !seen.Add(v))
                    {
                        // 找到之前出现相同数字的格子
                        for (int px = 0; px < x; px++)
                            if (g[px, y].Value == v) violations.Add(new Vector2Int(px, y));
                        violations.Add(new Vector2Int(x, y));
                    }
                }
            }

            // 检查每列
            for (int x = 0; x < 9; x++)
            {
                var seen = new HashSet<int>();
                for (int y = 0; y < 9; y++)
                {
                    int v = g[x, y].Value;
                    if (v > 0 && !seen.Add(v))
                    {
                        for (int py = 0; py < y; py++)
                            if (g[x, py].Value == v && !violations.Contains(new Vector2Int(x, py)))
                                violations.Add(new Vector2Int(x, py));
                        if (!violations.Contains(new Vector2Int(x, y)))
                            violations.Add(new Vector2Int(x, y));
                    }
                }
            }

            // 检查每宫（3×3）
            for (int bx = 0; bx < 3; bx++)
            {
                for (int by = 0; by < 3; by++)
                {
                    var seen = new HashSet<int>();
                    for (int dx = 0; dx < 3; dx++)
                    {
                        for (int dy = 0; dy < 3; dy++)
                        {
                            int x = bx * 3 + dx;
                            int y = by * 3 + dy;
                            int v = g[x, y].Value;
                            if (v > 0 && !seen.Add(v))
                            {
                                for (int pdx = 0; pdx < 3; pdx++)
                                    for (int pdy = 0; pdy < 3; pdy++)
                                    {
                                        int ppx = bx * 3 + pdx;
                                        int ppy = by * 3 + pdy;
                                        if (g[ppx, ppy].Value == v && !violations.Contains(new Vector2Int(ppx, ppy)))
                                            violations.Add(new Vector2Int(ppx, ppy));
                                    }
                            }
                        }
                    }
                }
            }

            return violations;
        }

        /// <summary>检查在 (x,y) 填入 digit 是否与已有数字冲突</summary>
        private bool HasConflict(int x, int y, int digit)
        {
            var g = _grid.Grid;

            // 检查同行
            for (int ix = 0; ix < 9; ix++)
                if (ix != x && g[ix, y].Value == digit) return true;

            // 检查同列
            for (int iy = 0; iy < 9; iy++)
                if (iy != y && g[x, iy].Value == digit) return true;

            // 检查同宫
            int bx = x / 3 * 3;
            int by = y / 3 * 3;
            for (int dx = 0; dx < 3; dx++)
                for (int dy = 0; dy < 3; dy++)
                {
                    int cx = bx + dx, cy = by + dy;
                    if (cx != x || cy != y)
                        if (g[cx, cy].Value == digit) return true;
                }

            return false;
        }
    }
