using System.Collections.Generic;

    /// <summary>
    /// 关卡数据库。MVP 使用硬编码数据。
    /// 后续可通过热更新从 CDN 加载新关卡。
    /// </summary>
    public static class LevelDatabase
    {
        /// <summary>每种游戏类型的关卡数量</summary>
        public const int LEVELS_PER_PUZZLE = 5;

        /// <summary>按类型获取关卡数量</summary>
        public static int GetLevelCount(PuzzleType type)
        {
            return LEVELS_PER_PUZZLE;
        }

        /// <summary>根据类型和索引创建关卡数据</summary>
        public static PuzzleLevelData CreateLevel(PuzzleType type, int levelIndex)
        {
            switch (type)
            {
                case PuzzleType.Sudoku:
                    return SudokuLevelData.CreateTest(levelIndex);
                case PuzzleType.Nurikabe:
                    return NurikabeLevelData.CreateTest(levelIndex);
                case PuzzleType.NumberLink:
                    return NumberLinkLevelData.CreateTest(levelIndex);
                case PuzzleType.HashiBridge:
                    return HashiBridgeLevelData.CreateTest(levelIndex);
                default:
                    return null;
            }
        }

        /// <summary>获取关卡选择列表数据（用于 UIList）</summary>
        public static List<LevelSelectItemData> GetLevelSelectItems(PuzzleType type)
        {
            var items = new List<LevelSelectItemData>();
            int count = GetLevelCount(type);
            int unlocked = SaveManager.GetUnlockedLevelCount(type);

            for (int i = 0; i < count; i++)
            {
                var entry = SaveManager.GetEntry(type, GetLevelId(type, i));
                items.Add(new LevelSelectItemData
                {
                    LevelIndex = i,
                    Label = $"第 {i + 1} 关",
                    IsUnlocked = i < unlocked,
                    IsCompleted = entry?.IsCompleted ?? false,
                    BestTime = entry?.BestTime ?? -1f,
                    LevelId = GetLevelId(type, i)
                });
            }

            return items;
        }

        /// <summary>生成关卡 ID</summary>
        public static string GetLevelId(PuzzleType type, int levelIndex)
        {
            switch (type)
            {
                case PuzzleType.Sudoku: return $"sudoku_{levelIndex}";
                case PuzzleType.Nurikabe: return $"nurikabe_{levelIndex}";
                case PuzzleType.NumberLink: return $"numlink_{levelIndex}";
                case PuzzleType.HashiBridge: return $"hashi_{levelIndex}";
                default: return $"unknown_{levelIndex}";
            }
        }

        /// <summary>获取游戏类型的中文名称</summary>
        public static string GetPuzzleName(PuzzleType type)
        {
            switch (type)
            {
                case PuzzleType.NumberLink: return "数回";
                case PuzzleType.Sudoku: return "数独";
                case PuzzleType.Nurikabe: return "数墙";
                case PuzzleType.HashiBridge: return "数桥";
                default: return "未知";
            }
        }

        /// <summary>获取游戏类型的描述</summary>
        public static string GetPuzzleDescription(PuzzleType type)
        {
            switch (type)
            {
                case PuzzleType.NumberLink:
                    return "连接相同数字对，填满所有格子";
                case PuzzleType.Sudoku:
                    return "在9×9棋盘上填入数字1-9";
                case PuzzleType.Nurikabe:
                    return "画出墙壁，形成数字大小的岛屿";
                case PuzzleType.HashiBridge:
                    return "在岛屿之间建造桥梁";
                default:
                    return "";
            }
        }
    }
