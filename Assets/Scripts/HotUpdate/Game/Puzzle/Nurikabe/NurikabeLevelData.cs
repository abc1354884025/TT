using System;
using System.Collections.Generic;
using UnityEngine;

    /// <summary>数墙关卡数据</summary>
    [Serializable]
    public class NurikabeLevelData : PuzzleLevelData
    {
        /// <summary>带数字的线索格，key=坐标，value=数字（岛屿大小）</summary>
        public Dictionary<Vector2Int, int> NumberedCells = new Dictionary<Vector2Int, int>();

        public NurikabeLevelData()
        {
            PuzzleType = PuzzleType.Nurikabe;
        }

        /// <summary>从字符串解析数墙关卡。
        /// 格式：每行用逗号分隔，不同行用分号分隔。
        /// 每个单元格：0=空, 1-9=数字线索。
        /// 示例："0,0,3,0,0;0,2,0,0,1;..."</summary>
        public override void Parse(string rawData)
        {
            NumberedCells.Clear();
            rawData = rawData.Trim();

            var rows = rawData.Split(';');
            GridHeight = rows.Length;

            for (int y = 0; y < rows.Length; y++)
            {
                var cols = rows[y].Split(',');
                GridWidth = Mathf.Max(GridWidth, cols.Length);

                for (int x = 0; x < cols.Length; x++)
                {
                    if (int.TryParse(cols[x].Trim(), out int val) && val > 0)
                        NumberedCells[new Vector2Int(x, y)] = val;
                }
            }
        }

        public static NurikabeLevelData CreateTest(int levelIndex)
        {
            string[] puzzles = new string[]
            {
                // 5×5 Level 1 (简单)
                "0,0,0,0,0;0,3,0,2,0;0,0,0,0,0;0,4,0,0,0;0,0,0,0,0",
                // 5×5 Level 2
                "0,2,0,0,0;0,0,0,4,0;1,0,0,0,0;0,0,2,0,0;0,0,0,0,3",
                // 7×7 Level 3
                "0,0,0,0,0,0,0;0,3,0,0,1,0,0;0,0,0,0,0,0,0;0,0,5,0,0,0,1;0,0,0,0,0,0,0;0,0,2,0,0,4,0;0,0,0,0,0,0,0",
                // 7×7 Level 4
                "0,0,2,0,0,0,0;0,0,0,0,3,0,0;1,0,0,0,0,0,2;0,0,0,4,0,0,0;3,0,0,0,0,0,0;0,0,1,0,0,0,0;0,0,0,0,5,0,0",
                // 10×10 Level 5
                "0,0,0,0,0,0,0,0,0,0;0,3,0,0,0,1,0,0,2,0;0,0,0,0,0,0,0,0,0,0;0,0,0,4,0,0,0,3,0,0;0,0,0,0,0,0,0,0,0,0;0,0,2,0,0,5,0,0,1,0;0,0,0,0,0,0,0,0,0,0;0,1,0,0,0,2,0,0,0,0;0,0,0,0,0,0,0,0,0,0;0,0,3,0,0,0,1,0,0,0"
            };

            var data = new NurikabeLevelData
            {
                LevelId = $"nurikabe_{levelIndex}",
                LevelIndex = levelIndex,
                Difficulty = levelIndex < 2 ? 1 : (levelIndex < 4 ? 3 : 5),
                DisplayName = $"数墙 - 第{levelIndex + 1}关"
            };

            if (levelIndex < puzzles.Length)
                data.Parse(puzzles[levelIndex]);
            else
                data.Parse(puzzles[0]);

            return data;
        }
    }
