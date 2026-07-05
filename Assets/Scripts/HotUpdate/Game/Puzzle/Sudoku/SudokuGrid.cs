using System;
using UnityEngine;

    /// <summary>数独格子数据</summary>
    [Serializable]
    public class SudokuCell : GridCellData
    {
        /// <summary>当前填入的数字（1-9，0 表示空）</summary>
        public int Value;

        /// <summary>是否为预设线索格</summary>
        public bool IsClue;

        /// <summary>是否违反规则（用于高亮）</summary>
        public bool IsError;

        /// <summary>候选笔记（铅笔标记），MVP 可省略。每个 bit 代表一个数字 1-9。</summary>
        public int CandidateNotes;

        public override void ResetToDefault()
        {
            if (!IsLocked)
            {
                Value = 0;
                IsError = false;
                CandidateNotes = 0;
            }
        }

        public override object Clone()
        {
            return new SudokuCell
            {
                Position = Position,
                IsLocked = IsLocked,
                Value = Value,
                IsClue = IsClue,
                IsError = IsError,
                CandidateNotes = CandidateNotes
            };
        }
    }

    /// <summary>数独棋盘</summary>
    public class SudokuGrid
    {
        public PuzzleGrid<SudokuCell> Grid { get; private set; }
        public const int SIZE = 9;
        public const int BOX_SIZE = 3;

        public SudokuGrid()
        {
            Grid = new PuzzleGrid<SudokuCell>(SIZE, SIZE);
        }

        /// <summary>根据关卡数据初始化（设置线索）</summary>
        public void Initialize(int[,] clues)
        {
            for (int x = 0; x < SIZE; x++)
                for (int y = 0; y < SIZE; y++)
                {
                    int clue = clues[x, y];
                    var cell = Grid[x, y];
                    if (clue > 0)
                    {
                        cell.Value = clue;
                        cell.IsClue = true;
                        cell.IsLocked = true;
                    }
                    else
                    {
                        cell.Value = 0;
                        cell.IsClue = false;
                        cell.IsLocked = false;
                    }
                    cell.IsError = false;
                }
        }

        /// <summary>在指定位置填入数字。返回是否成功。</summary>
        public bool SetValue(int x, int y, int value)
        {
            if (!Grid.InBounds(x, y)) return false;
            var cell = Grid[x, y];
            if (cell.IsClue) return false;

            // 如果点同一个数字则擦除
            if (cell.Value == value)
                cell.Value = 0;
            else
                cell.Value = value;

            return true;
        }

        /// <summary>获取当前所有用户填入的值（用于存档）</summary>
        public int[,] GetPlayerValues()
        {
            var values = new int[SIZE, SIZE];
            for (int x = 0; x < SIZE; x++)
                for (int y = 0; y < SIZE; y++)
                    values[x, y] = Grid[x, y].IsClue ? 0 : Grid[x, y].Value;
            return values;
        }

        /// <summary>检查是否所有格子都已填满</summary>
        public bool IsFilled()
        {
            for (int x = 0; x < SIZE; x++)
                for (int y = 0; y < SIZE; y++)
                    if (Grid[x, y].Value == 0) return false;
            return true;
        }
    }
