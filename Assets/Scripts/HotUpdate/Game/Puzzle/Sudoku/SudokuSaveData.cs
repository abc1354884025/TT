using System;
using UnityEngine;

    /// <summary>数独存档数据（可序列化为 JSON）</summary>
    [Serializable]
    public class SudokuSaveData
    {
        /// <summary>9×9 玩家填入值（按行优先顺序存储，81 个 int）</summary>
        public int[] PlayerValues = new int[81];

        /// <summary>将二维数组扁平化为 int[]</summary>
        public void FromGrid(int[,] values)
        {
            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 9; y++)
                    PlayerValues[y * 9 + x] = values[x, y];
        }

        /// <summary>从 int[] 恢复到二维数组</summary>
        public int[,] ToGrid()
        {
            var values = new int[9, 9];
            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 9; y++)
                    values[x, y] = PlayerValues[y * 9 + x];
            return values;
        }

        public string ToJson() => JsonUtility.ToJson(this);
        public static SudokuSaveData FromJson(string json) => JsonUtility.FromJson<SudokuSaveData>(json);
    }
