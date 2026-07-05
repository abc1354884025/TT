using UnityEngine;

    /// <summary>
    /// Puzzle 操作基类。GridInputHandler 直接实例化此类型传递输入事件，
    /// 每种游戏的操作类型（SudokuMove 等）继承此类以携带额外数据。
    /// </summary>
    public class PuzzleMove
    {
        /// <summary>操作类型（用于撤销时判断反操作逻辑）</summary>
        public InputActionType ActionType;

        /// <summary>主位置</summary>
        public Vector2Int Position;

        /// <summary>副位置（拖拽终点等）</summary>
        public Vector2Int? SecondaryPos;

        /// <summary>通用值载荷（数字、桥数量等）</summary>
        public int Value;

        /// <summary>旧值（用于撤销还原）</summary>
        public int OldValue;
    }

    /// <summary>数独操作：在指定位置填入/擦除数字</summary>
    public class SudokuMove : PuzzleMove
    {
        /// <summary>填入的数字（0 = 擦除）</summary>
        public int Digit => Value;
    }

    /// <summary>数墙操作：在指定位置切换白/黑</summary>
    public class NurikabeMove : PuzzleMove
    {
        /// <summary>切换前的状态</summary>
        public NurikabeCellState PreviousState => (NurikabeCellState)OldValue;

        /// <summary>切换后的状态</summary>
        public NurikabeCellState NewState => (NurikabeCellState)Value;
    }

    /// <summary>数回操作：在路径上添加/删除格子</summary>
    public class NumberLinkMove : PuzzleMove
    {
        /// <summary>进方向</summary>
        public PuzzleDirection InDir;

        /// <summary>出方向</summary>
        public PuzzleDirection OutDir;

        /// <summary>路径所属的数字值</summary>
        public int NumberValue => Value;
    }

    /// <summary>数桥操作：在两个岛之间添加/移除桥</summary>
    public class HashiBridgeMove : PuzzleMove
    {
        /// <summary>桥的数量变化量（+1 或 -1 或 +2）</summary>
        public int BridgeCountDelta => Value;
    }
