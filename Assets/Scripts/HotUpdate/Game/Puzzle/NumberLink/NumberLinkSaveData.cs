using System;
using UnityEngine;

    /// <summary>数回存档数据</summary>
    [Serializable]
    public class NumberLinkSaveData
    {
        public int[] CellValues;    // 扁平化的 NumberValue
        public int[] InDirs;        // 扁平化的 (int)PuzzleDirection
        public int[] OutDirs;
        public int[] ColorIndices;
        public int Width;
        public int Height;

        public void FromGrid(NumberLinkGrid grid)
        {
            Width = grid.Grid.Width;
            Height = grid.Grid.Height;
            int total = Width * Height;
            CellValues = new int[total];
            InDirs = new int[total];
            OutDirs = new int[total];
            ColorIndices = new int[total];

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    int i = y * Width + x;
                    var cell = grid.Grid[x, y];
                    CellValues[i] = cell.NumberValue;
                    InDirs[i] = (int)cell.InDir;
                    OutDirs[i] = (int)cell.OutDir;
                    ColorIndices[i] = cell.ColorIndex;
                }
        }

        public void ToGrid(NumberLinkGrid grid)
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    int i = y * Width + x;
                    var cell = grid.Grid[x, y];
                    if (!cell.IsEndpoint)
                    {
                        cell.NumberValue = CellValues[i];
                        cell.InDir = (PuzzleDirection)InDirs[i];
                        cell.OutDir = (PuzzleDirection)OutDirs[i];
                        cell.ColorIndex = ColorIndices[i];
                    }
                }
        }

        public string ToJson() => JsonUtility.ToJson(this);
        public static NumberLinkSaveData FromJson(string json) => JsonUtility.FromJson<NumberLinkSaveData>(json);
    }
