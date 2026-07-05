using System;
using UnityEngine;

    /// <summary>
    /// 存档管理器。使用 PlayerPrefs + JsonUtility 持久化存档数据。
    /// 抽象为清晰接口，后续可替换为文件存储或远程存储。
    /// </summary>
    public static class SaveManager
    {
        private const string SAVE_KEY = "PuzzleGameSaveData";
        private static SaveData _current;

        /// <summary>当前存档数据（内存缓存）</summary>
        public static SaveData Current
        {
            get
            {
                if (_current == null) Load();
                return _current;
            }
        }

        /// <summary>从 PlayerPrefs 加载存档</summary>
        public static void Load()
        {
            string json = PlayerPrefs.GetString(SAVE_KEY, "");
            if (string.IsNullOrEmpty(json))
            {
                _current = new SaveData();
                Debug.Log("[SaveManager] 无存档数据，创建新存档");
            }
            else
            {
                try
                {
                    _current = JsonUtility.FromJson<SaveData>(json);
                    if (_current == null) _current = new SaveData();
                    Debug.Log($"[SaveManager] 存档加载成功，共 {_current.Entries.Count} 条记录");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SaveManager] 存档解析失败: {e.Message}，创建新存档");
                    _current = new SaveData();
                }
            }
        }

        /// <summary>保存到 PlayerPrefs</summary>
        public static void Save()
        {
            if (_current == null) return;
            try
            {
                string json = JsonUtility.ToJson(_current);
                PlayerPrefs.SetString(SAVE_KEY, json);
                PlayerPrefs.Save();
                Debug.Log("[SaveManager] 存档已保存");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] 存档保存失败: {e.Message}");
            }
        }

        /// <summary>保存某个益智游戏的关卡状态</summary>
        public static void SavePuzzleState(PuzzleType type, string levelId, string rawState, bool isSolved, float elapsedTime, int moveCount)
        {
            var entry = Current.GetOrCreateEntry(type, levelId);
            entry.RawSaveState = rawState;

            if (isSolved)
            {
                entry.IsCompleted = true;
                if (elapsedTime < entry.BestTime || entry.BestTime < 0)
                    entry.BestTime = elapsedTime;
                if (moveCount < entry.BestMoves)
                    entry.BestMoves = moveCount;
            }

            Save();
        }

        /// <summary>获取某游戏的已解锁关卡数</summary>
        public static int GetUnlockedLevelCount(PuzzleType type)
        {
            return Current.UnlockedLevels.TryGetValue(type, out int count) ? count : 1;
        }

        /// <summary>解锁下一个关卡</summary>
        public static void UnlockNextLevel(PuzzleType type, int currentLevelIndex)
        {
            int nextLevel = currentLevelIndex + 1;
            if (Current.UnlockedLevels.TryGetValue(type, out int unlocked) && unlocked <= nextLevel)
            {
                Current.UnlockedLevels[type] = nextLevel + 1; // +1 因为存储的是数量
                Save();
            }
        }

        /// <summary>获取某关卡的存档条目</summary>
        public static PuzzleSaveEntry GetEntry(PuzzleType type, string levelId)
        {
            return Current.Entries.Find(e => e.PuzzleType == type && e.LevelId == levelId);
        }

        /// <summary>清除所有存档数据</summary>
        public static void ClearAll()
        {
            _current = new SaveData();
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
            Debug.Log("[SaveManager] 所有存档已清除");
        }
    }
