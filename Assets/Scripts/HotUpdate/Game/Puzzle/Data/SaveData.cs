using System;
using System.Collections.Generic;

    /// <summary>单个游戏关卡的存档条目</summary>
    [Serializable]
    public class PuzzleSaveEntry
    {
        /// <summary>关卡ID</summary>
        public string LevelId;

        /// <summary>游戏类型</summary>
        public PuzzleType PuzzleType;

        /// <summary>是否已完成</summary>
        public bool IsCompleted;

        /// <summary>最佳用时（秒），-1 = 未完成过</summary>
        public float BestTime = -1f;

        /// <summary>最小步数</summary>
        public int BestMoves = int.MaxValue;

        /// <summary>游戏特定存档状态（JSON 字符串）</summary>
        public string RawSaveState;
    }

    /// <summary>根存档容器</summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>每个游戏的关卡解锁状态</summary>
        public Dictionary<PuzzleType, int> UnlockedLevels = new Dictionary<PuzzleType, int>
        {
            { PuzzleType.NumberLink, 1 },
            { PuzzleType.Sudoku, 1 },
            { PuzzleType.Nurikabe, 1 },
            { PuzzleType.HashiBridge, 1 }
        };

        /// <summary>所有已保存的关卡条目列表</summary>
        public List<PuzzleSaveEntry> Entries = new List<PuzzleSaveEntry>();

        /// <summary>获取或创建某个关卡的存档条目</summary>
        public PuzzleSaveEntry GetOrCreateEntry(PuzzleType type, string levelId)
        {
            var entry = Entries.Find(e => e.PuzzleType == type && e.LevelId == levelId);
            if (entry == null)
            {
                entry = new PuzzleSaveEntry { PuzzleType = type, LevelId = levelId };
                Entries.Add(entry);
            }
            return entry;
        }

        /// <summary>获取某游戏的已完成关卡数</summary>
        public int GetCompletedCount(PuzzleType type)
        {
            int count = 0;
            foreach (var e in Entries)
                if (e.PuzzleType == type && e.IsCompleted) count++;
            return count;
        }

        /// <summary>获取总关卡进度摘要</summary>
        public string GetProgressSummary(int totalLevelsPerPuzzle)
        {
            int total = 0;
            foreach (PuzzleType type in Enum.GetValues(typeof(PuzzleType)))
                total += GetCompletedCount(type);
            return $"已完成 {total}/{totalLevelsPerPuzzle * 4}";
        }
    }
