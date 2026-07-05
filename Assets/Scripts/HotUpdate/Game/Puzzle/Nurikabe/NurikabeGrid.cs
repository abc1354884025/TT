using System;
using System.Collections.Generic;
using UnityEngine;

    /// <summary>数墙格子数据</summary>
    [Serializable]
    public class NurikabeCell : GridCellData
    {
        /// <summary>当前状态</summary>
        public NurikabeCellState State = NurikabeCellState.White;

        /// <summary>数字值（仅 NumberedWhite 有效）</summary>
        public int NumberValue;

        /// <summary>所属岛屿 ID（-1 = 无）</summary>
        public int IslandId = -1;

        public override void ResetToDefault()
        {
            if (!IsLocked)
            {
                State = NurikabeCellState.White;
                IslandId = -1;
            }
        }

        public override object Clone()
        {
            return new NurikabeCell
            {
                Position = Position,
                IsLocked = IsLocked,
                State = State,
                NumberValue = NumberValue,
                IslandId = IslandId
            };
        }
    }

    /// <summary>数墙棋盘</summary>
    public class NurikabeGrid
    {
        public PuzzleGrid<NurikabeCell> Grid { get; private set; }

        public NurikabeGrid(int width, int height)
        {
            Grid = new PuzzleGrid<NurikabeCell>(width, height);
        }

        /// <summary>根据关卡数据初始化（放置数字线索）</summary>
        public void Initialize(Dictionary<Vector2Int, int> numberedCells)
        {
            foreach (var kv in numberedCells)
            {
                var cell = Grid[kv.Key];
                cell.State = NurikabeCellState.NumberedWhite;
                cell.NumberValue = kv.Value;
                cell.IsLocked = true;
            }
        }

        /// <summary>切换指定位置的白色/黑色状态。返回是否成功。</summary>
        public bool Toggle(int x, int y)
        {
            if (!Grid.InBounds(x, y)) return false;
            var cell = Grid[x, y];
            if (cell.IsLocked) return false;

            cell.State = cell.State == NurikabeCellState.Black
                ? NurikabeCellState.White
                : NurikabeCellState.Black;

            return true;
        }

        /// <summary>获取当前所有格子的状态（用于存档）</summary>
        public int[,] GetCellStates()
        {
            var w = Grid.Width;
            var h = Grid.Height;
            var states = new int[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    states[x, y] = (int)Grid[x, y].State;
            return states;
        }
    }
