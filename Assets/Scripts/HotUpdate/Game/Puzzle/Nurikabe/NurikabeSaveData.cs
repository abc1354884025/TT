using System;
using UnityEngine;

    /// <summary>数墙存档数据</summary>
    [Serializable]
    public class NurikabeSaveData
    {
        public int[] CellStates;   // 扁平化的格子状态 (int)NurikabeCellState
        public int[] NumberValues;  // 线索数字
        public int[] IslandIds;     // 岛屿 ID
        public int Width;
        public int Height;

        public void FromGrid(NurikabeGrid grid)
        {
            Width = grid.Grid.Width;
            Height = grid.Grid.Height;
            int total = Width * Height;
            CellStates = new int[total];
            NumberValues = new int[total];
            IslandIds = new int[total];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int i = y * Width + x;
                    var cell = grid.Grid[x, y];
                    CellStates[i] = (int)cell.State;
                    NumberValues[i] = cell.NumberValue;
                    IslandIds[i] = cell.IslandId;
                }
            }
        }

        public void ToGrid(NurikabeGrid grid)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    int i = y * Width + x;
                    var cell = grid.Grid[x, y];
                    if (!cell.IsLocked)
                        cell.State = (NurikabeCellState)CellStates[i];
                }
            }
        }

        public string ToJson() => JsonUtility.ToJson(this);
        public static NurikabeSaveData FromJson(string json) => JsonUtility.FromJson<NurikabeSaveData>(json);
    }
