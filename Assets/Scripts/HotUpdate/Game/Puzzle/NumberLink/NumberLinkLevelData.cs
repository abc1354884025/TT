using System;
using System.Collections.Generic;
using UnityEngine;

    /// <summary>数回关卡数据</summary>
    [Serializable]
    public class NumberLinkLevelData : PuzzleLevelData
    {
        /// <summary>数字配对列表：每个元素为 (数字值, 端点1坐标, 端点2坐标)</summary>
        public List<NumberPair> Pairs = new List<NumberPair>();

        public NumberLinkLevelData()
        {
            PuzzleType = PuzzleType.NumberLink;
        }

        /// <summary>从字符串解析数回关卡。
        /// 格式："size:5x5;1:(0,0)-(4,4);2:(1,0)-(0,4);..."
        /// 每行定义一个配对。</summary>
        public override void Parse(string rawData)
        {
            Pairs.Clear();
            rawData = rawData.Trim();

            var lines = rawData.Split(';');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("size:"))
                {
                    var parts = trimmed.Substring(5).Split('x');
                    if (parts.Length == 2)
                    {
                        GridWidth = int.Parse(parts[0]);
                        GridHeight = int.Parse(parts[1]);
                    }
                }
                else if (trimmed.Contains(":"))
                {
                    var colonIdx = trimmed.IndexOf(':');
                    int value = int.Parse(trimmed.Substring(0, colonIdx));
                    var coords = trimmed.Substring(colonIdx + 1).Split('-');
                    if (coords.Length == 2)
                    {
                        Pairs.Add(new NumberPair
                        {
                            Value = value,
                            Pos1 = ParseCoord(coords[0]),
                            Pos2 = ParseCoord(coords[1])
                        });
                    }
                }
            }
        }

        private Vector2Int ParseCoord(string s)
        {
            s = s.Trim().Trim('(').Trim(')');
            var parts = s.Split(',');
            return new Vector2Int(int.Parse(parts[0]), int.Parse(parts[1]));
        }

        [Serializable]
        public class NumberPair
        {
            public int Value;
            public Vector2Int Pos1;
            public Vector2Int Pos2;
        }

        public static NumberLinkLevelData CreateTest(int levelIndex)
        {
            string[] puzzles = new string[]
            {
                // 5×5 Level 1 (2对数字)
                "size:5x5;1:(0,0)-(4,4);2:(1,0)-(0,4)",
                // 5×5 Level 2 (3对)
                "size:5x5;1:(0,0)-(2,4);2:(4,0)-(0,4);3:(2,2)-(4,2)",
                // 7×7 Level 3 (4对)
                "size:7x7;1:(0,1)-(6,5);2:(1,0)-(5,6);3:(3,1)-(3,5);4:(0,6)-(6,0)",
                // 7×7 Level 4 (5对)
                "size:7x7;1:(0,0)-(6,6);2:(1,0)-(0,5);3:(6,0)-(2,6);4:(3,1)-(3,4);5:(5,2)-(1,5)",
                // 10×10 Level 5 (6对)
                "size:10x10;1:(0,0)-(9,9);2:(0,9)-(9,0);3:(2,2)-(7,7);4:(5,0)-(5,9);5:(0,5)-(9,5);6:(2,7)-(7,2)"
            };

            var data = new NumberLinkLevelData
            {
                LevelId = $"numlink_{levelIndex}",
                LevelIndex = levelIndex,
                Difficulty = levelIndex < 2 ? 1 : (levelIndex < 4 ? 3 : 5),
                DisplayName = $"数回 - 第{levelIndex + 1}关"
            };

            if (levelIndex < puzzles.Length)
                data.Parse(puzzles[levelIndex]);
            else
                data.Parse(puzzles[0]);

            return data;
        }
    }
