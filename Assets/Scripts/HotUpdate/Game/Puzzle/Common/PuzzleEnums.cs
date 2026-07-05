using System;
using UnityEngine;

    /// <summary>四种益智游戏类型</summary>
    public enum PuzzleType
    {
        NumberLink = 0,   // 数回
        Sudoku = 1,       // 数独
        Nurikabe = 2,     // 数墙
        HashiBridge = 3   // 数桥
    }

    /// <summary>方向枚举（用于路径绘制）</summary>
    public enum PuzzleDirection
    {
        None = 0,
        Up = 1,
        Down = 2,
        Left = 3,
        Right = 4
    }

    /// <summary>数墙格子状态</summary>
    public enum NurikabeCellState
    {
        White = 0,          // 白色（岛屿部分）
        Black = 1,          // 黑色（墙壁部分）
        NumberedWhite = 2   // 带数字的白色（线索格，不可改变）
    }

    /// <summary>数桥格子类型</summary>
    public enum HashiCellType
    {
        Sea = 0,    // 海
        Island = 1  // 岛
    }

    /// <summary>数桥的桥记录（用于存档）</summary>
    [Serializable]
    public struct BridgeRecord
    {
        public int X1, Y1;  // 岛A的坐标
        public int X2, Y2;  // 岛B的坐标
        public int Count;   // 桥的数量（1 或 2）

        public BridgeRecord(int x1, int y1, int x2, int y2, int count)
        {
            X1 = x1; Y1 = y1; X2 = x2; Y2 = y2; Count = count;
        }

        public (Vector2Int, Vector2Int) Key()
        {
            var a = new Vector2Int(X1, Y1);
            var b = new Vector2Int(X2, Y2);
            // 规范化键：始终按字典序排列
            return a.x < b.x || (a.x == b.x && a.y < b.y) ? (a, b) : (b, a);
        }
    }

    /// <summary>输入动作类型</summary>
    public enum InputActionType
    {
        Tap,            // 单击
        DragStart,      // 拖拽开始
        DragEnter,       // 拖拽过程中经过某个格子
        DragEnd         // 拖拽结束
    }
