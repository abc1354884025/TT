using System;

    /// <summary>数独关卡数据</summary>
    [Serializable]
    public class SudokuLevelData : PuzzleLevelData
    {
        /// <summary>9×9 线索棋盘（0 = 空白）</summary>
        public int[,] Clues;

        public SudokuLevelData()
        {
            PuzzleType = PuzzleType.Sudoku;
            GridWidth = 9;
            GridHeight = 9;
            Clues = new int[9, 9];
        }

        /// <summary>从 81 字符的字符串解析数独关卡。
        /// 格式：每行9个字符，"530070000260015..." 共 81 字符，
        /// 0 或 . 表示空白。</summary>
        public override void Parse(string rawData)
        {
            rawData = rawData.Replace(".", "0").Replace("\n", "").Replace(" ", "").Trim();
            if (rawData.Length < 81)
                throw new ArgumentException($"数独关卡数据长度不足: {rawData.Length}（需要81字符）");

            for (int i = 0; i < 81; i++)
            {
                int x = i % 9;
                int y = i / 9;
                Clues[x, y] = rawData[i] >= '0' && rawData[i] <= '9' ? rawData[i] - '0' : 0;
            }
        }

        /// <summary>创建简单测试关卡</summary>
        public static SudokuLevelData CreateTest(int levelIndex)
        {
            // 若干预设关卡（81 字符字符串，0 表示空白）
            string[] puzzles = new string[]
            {
                // 简单 Level 1
                "530070000600195000098000060800060003400803001700020006060000280000419005000080079",
                // 简单 Level 2
                "000000907000420180000705026100904000050000040000507009920108000034059010507000000",
                // 中等 Level 3
                "020000006059000700700400000005200008200070004800009100000006003007000240600000080",
                // 中等 Level 4
                "000030002100080000000700500004000009010204030800000600007005000000090003200060000",
                // 困难 Level 5
                "000000080002000300090001000600000100007080000500020009000400020001070000040000010",
            };

            var data = new SudokuLevelData
            {
                LevelId = $"sudoku_{levelIndex}",
                LevelIndex = levelIndex,
                Difficulty = levelIndex < 2 ? 1 : (levelIndex < 4 ? 3 : 5),
                DisplayName = $"数独 - 第{levelIndex + 1}关"
            };

            if (levelIndex < puzzles.Length)
                data.Parse(puzzles[levelIndex]);
            else
                data.Parse(puzzles[0]);

            return data;
        }
    }
