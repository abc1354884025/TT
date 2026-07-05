using System;
using System.Collections.Generic;
using UnityEngine;

    /// <summary>数桥关卡数据</summary>
    [Serializable]
    public class HashiBridgeLevelData : PuzzleLevelData
    {
        /// <summary>岛屿列表：每个岛屿的坐标和所需桥数量</summary>
        public List<IslandData> Islands = new List<IslandData>();

        public HashiBridgeLevelData()
        {
            PuzzleType = PuzzleType.HashiBridge;
        }

        /// <summary>从字符串解析数桥关卡。
        /// 格式："size:7x7;islands:(1,2):3,(4,1):2,(5,5):4,..."
        /// 每个岛定义为 (x,y):requiredBridges。</summary>
        public override void Parse(string rawData)
        {
            Islands.Clear();
            rawData = rawData.Trim();

            var parts = rawData.Split(';');
            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (trimmed.StartsWith("size:"))
                {
                    var dims = trimmed.Substring(5).Split('x');
                    GridWidth = int.Parse(dims[0]);
                    GridHeight = int.Parse(dims[1]);
                }
                else if (trimmed.StartsWith("islands:"))
                {
                    var islandDefs = trimmed.Substring(8).Split(',');
                    for (int i = 0; i < islandDefs.Length; i += 2)
                    {
                        if (i + 1 >= islandDefs.Length) break;
                        var coordStr = islandDefs[i].Trim().Trim('(').Trim(')');
                        var xy = coordStr.Split(':')[0].Split('/');  // 支持 (x:y) 和 (x/y)
                        int x = int.Parse(xy[0]);
                        int y = int.Parse(xy[1]);
                        int req = int.Parse(islandDefs[i + 1].Trim());

                        Islands.Add(new IslandData
                        {
                            Position = new Vector2Int(x, y),
                            RequiredBridges = req
                        });
                    }
                }
            }
        }

        [Serializable]
        public class IslandData
        {
            public Vector2Int Position;
            public int RequiredBridges;
        }

        public static HashiBridgeLevelData CreateTest(int levelIndex)
        {
            // 简化格式: "size:WxH;islands:x:y:req,x:y:req,..."
            string[] puzzles = new string[]
            {
                // 5×5 Level 1 (3个岛)
                "size:5x5;islands:0:0:2,4:0:2,2:4:3",
                // 5×5 Level 2 (4个岛)
                "size:5x5;islands:0:1:2,4:1:2,0:4:3,4:3:3",
                // 7×7 Level 3 (5个岛)
                "size:7x7;islands:1:1:3,5:1:2,1:5:4,6:5:3,3:3:2",
                // 7×7 Level 4 (6个岛)
                "size:7x7;islands:0:2:2,3:0:3,6:2:2,0:5:3,3:6:2,6:4:4",
                // 10×10 Level 5 (8个岛)
                "size:10x10;islands:1:1:3,8:1:2,1:8:4,8:8:3,4:3:3,5:3:2,4:6:2,5:6:4"
            };

            var data = new HashiBridgeLevelData
            {
                LevelId = $"hashi_{levelIndex}",
                LevelIndex = levelIndex,
                Difficulty = levelIndex < 2 ? 1 : (levelIndex < 4 ? 3 : 5),
                DisplayName = $"数桥 - 第{levelIndex + 1}关"
            };

            if (levelIndex < puzzles.Length)
                data.Parse(puzzles[levelIndex]);
            else
                data.Parse(puzzles[0]);

            return data;
        }
    }
