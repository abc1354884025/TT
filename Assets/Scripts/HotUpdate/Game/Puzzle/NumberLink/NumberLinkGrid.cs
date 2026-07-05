using System;
using System.Collections.Generic;
using UnityEngine;

    /// <summary>数回格子数据</summary>
    [Serializable]
    public class NumberLinkCell : GridCellData
    {
        /// <summary>数字值（0 = 空路径格, >0 = 端点或路径颜色标识）</summary>
        public int NumberValue;

        /// <summary>是否为数字端点</summary>
        public bool IsEndpoint;

        /// <summary>路径进入方向</summary>
        public PuzzleDirection InDir = PuzzleDirection.None;

        /// <summary>路径离开方向</summary>
        public PuzzleDirection OutDir = PuzzleDirection.None;

        /// <summary>路径颜色索引（用于渲染不同颜色）</summary>
        public int ColorIndex = -1;

        public bool HasPath => InDir != PuzzleDirection.None || OutDir != PuzzleDirection.None;

        public override void ResetToDefault()
        {
            if (!IsLocked)
            {
                if (!IsEndpoint)
                    NumberValue = 0;
                InDir = PuzzleDirection.None;
                OutDir = PuzzleDirection.None;
                ColorIndex = -1;
            }
        }

        public override object Clone()
        {
            return new NumberLinkCell
            {
                Position = Position,
                IsLocked = IsLocked,
                NumberValue = NumberValue,
                IsEndpoint = IsEndpoint,
                InDir = InDir,
                OutDir = OutDir,
                ColorIndex = ColorIndex
            };
        }
    }

    /// <summary>数回棋盘</summary>
    public class NumberLinkGrid
    {
        public PuzzleGrid<NumberLinkCell> Grid { get; private set; }
        public readonly Dictionary<int, List<Vector2Int>> ActivePaths = new Dictionary<int, List<Vector2Int>>();

        /// <summary>预定义的端点颜色（最多8组配对）</summary>
        public static readonly Color[] PathColors = new Color[]
        {
            new Color(0.90f, 0.30f, 0.30f),  // 红
            new Color(0.30f, 0.55f, 0.90f), // 蓝
            new Color(0.30f, 0.80f, 0.35f),  // 绿
            new Color(0.95f, 0.65f, 0.15f), // 橙
            new Color(0.70f, 0.30f, 0.85f), // 紫
            new Color(0.95f, 0.40f, 0.55f), // 粉
            new Color(0.30f, 0.75f, 0.80f), // 青
            new Color(0.65f, 0.50f, 0.35f), // 棕
        };

        public NumberLinkGrid(int width, int height)
        {
            Grid = new PuzzleGrid<NumberLinkCell>(width, height);
        }

        /// <summary>根据关卡数据初始化（放置端点）</summary>
        public void Initialize((int value, Vector2Int pos1, Vector2Int pos2)[] pairs)
        {
            for (int i = 0; i < pairs.Length; i++)
            {
                var (value, pos1, pos2) = pairs[i];
                var c1 = Grid[pos1];
                c1.NumberValue = value;
                c1.IsEndpoint = true;
                c1.IsLocked = true;

                var c2 = Grid[pos2];
                c2.NumberValue = value;
                c2.IsEndpoint = true;
                c2.IsLocked = true;
            }
        }

        /// <summary>在路径上添加/延伸一个格子</summary>
        public bool AddToPath(Vector2Int pos, int numberValue, PuzzleDirection fromDir, PuzzleDirection toDir)
        {
            if (!Grid.InBounds(pos)) return false;
            var cell = Grid[pos];
            if (cell.IsEndpoint && cell.NumberValue != numberValue) return false;

            cell.NumberValue = numberValue;
            cell.InDir = fromDir;
            cell.OutDir = toDir;

            if (!ActivePaths.ContainsKey(numberValue))
                ActivePaths[numberValue] = new List<Vector2Int>();
            if (!ActivePaths[numberValue].Contains(pos))
                ActivePaths[numberValue].Add(pos);

            return true;
        }

        /// <summary>清除某条路径上的一个格子</summary>
        public bool RemoveFromPath(Vector2Int pos, int numberValue)
        {
            if (!Grid.InBounds(pos)) return false;
            var cell = Grid[pos];
            if (cell.IsEndpoint) return false;

            cell.NumberValue = 0;
            cell.InDir = PuzzleDirection.None;
            cell.OutDir = PuzzleDirection.None;
            cell.ColorIndex = -1;

            ActivePaths.TryGetValue(numberValue, out var list);
            list?.Remove(pos);

            return true;
        }

        /// <summary>检查是否所有格子都被路径覆盖</summary>
        public bool IsFullyCovered()
        {
            for (int x = 0; x < Grid.Width; x++)
                for (int y = 0; y < Grid.Height; y++)
                    if (Grid[x, y].NumberValue == 0) return false;
            return true;
        }

        /// <summary>获取存档数据</summary>
        public int[,] GetCellValues()
        {
            var values = new int[Grid.Width, Grid.Height];
            for (int x = 0; x < Grid.Width; x++)
                for (int y = 0; y < Grid.Height; y++)
                    values[x, y] = Grid[x, y].NumberValue;
            return values;
        }
    }
