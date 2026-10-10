using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 存档模块——使用 PlayerPrefs 保存 JSON，不依赖特定平台 SDK。
/// 用法：
///   SaveManager.Load();           // 游戏启动时加载
///   SaveManager.Save();           // 关键节点存档
///   SaveManager.SetInt("gold", 100);
///   int gold = SaveManager.GetInt("gold");
/// </summary>
public static class SaveManager
{
    private const string SaveKey = "game_save_v1";
    private const int CurrentVersion = 1;

    /// <summary>当前存档数据</summary>
    public static GameSaveData Data { get; private set; }

    // ====== 快捷存取 ======

    public static int GetInt(string key, int defaultValue = 0)
        => Data?.ints.TryGetValue(key, out var v) == true ? v : defaultValue;

    public static void SetInt(string key, int value)
        { if (Data != null) Data.ints[key] = value; }

    public static float GetFloat(string key, float defaultValue = 0f)
        => Data?.floats.TryGetValue(key, out var v) == true ? v : defaultValue;

    public static void SetFloat(string key, float value)
        { if (Data != null) Data.floats[key] = value; }

    public static string GetString(string key, string defaultValue = "")
        => Data?.strings.TryGetValue(key, out var v) == true ? v : defaultValue;

    public static void SetString(string key, string value)
        { if (Data != null) Data.strings[key] = value; }

    // ====== 生命周期 ======

    /// <summary>从底层加载存档（游戏启动时调用）</summary>
    public static void Load()
    {
        try
        {
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                Data = new GameSaveData();
                Debug.Log("[SaveManager] 无存档，创建新存档");
                return;
            }

            var json = PlayerPrefs.GetString(SaveKey, string.Empty);
            var envelope = JsonUtility.FromJson<SaveEnvelope>(json);
            Data = envelope?.ToGameSaveData() ?? new GameSaveData();

            if (envelope == null)
            {
                Debug.LogWarning("[SaveManager] 存档内容为空，使用默认存档");
                return;
            }

            Debug.Log($"[SaveManager] 加载存档成功，版本: {envelope.version}，金币: {GetInt("gold", 0)}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] 加载失败，使用默认存档: {e.Message}");
            Data = new GameSaveData();
        }
    }

    /// <summary>保存到底层（关关卡/购买后/返回主菜单时调用）</summary>
    public static void Save()
    {
        if (Data == null) Data = new GameSaveData();

        try
        {
            var envelope = SaveEnvelope.From(Data);
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(envelope));
            PlayerPrefs.Save();
            Debug.Log("[SaveManager] 保存成功");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] 保存异常: {e.Message}");
        }
    }

    /// <summary>删除所有存档</summary>
    public static void Delete()
    {
        try
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            Data = new GameSaveData();
            Debug.Log("[SaveManager] 存档已删除");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] 删除异常: {e.Message}");
        }
    }

    [Serializable]
    private sealed class SaveEnvelope
    {
        public int version = CurrentVersion;
        public List<IntEntry> ints = new List<IntEntry>();
        public List<FloatEntry> floats = new List<FloatEntry>();
        public List<StringEntry> strings = new List<StringEntry>();
        public List<string> completedLevels = new List<string>();

        public static SaveEnvelope From(GameSaveData data)
        {
            var envelope = new SaveEnvelope();

            if (data.ints != null)
                foreach (var pair in data.ints)
                    envelope.ints.Add(new IntEntry { key = pair.Key, value = pair.Value });

            if (data.floats != null)
                foreach (var pair in data.floats)
                    envelope.floats.Add(new FloatEntry { key = pair.Key, value = pair.Value });

            if (data.strings != null)
                foreach (var pair in data.strings)
                    envelope.strings.Add(new StringEntry { key = pair.Key, value = pair.Value });

            if (data.completedLevels != null)
                envelope.completedLevels.AddRange(data.completedLevels);

            return envelope;
        }

        public GameSaveData ToGameSaveData()
        {
            var data = new GameSaveData();

            if (ints != null)
                foreach (var entry in ints)
                    if (!string.IsNullOrEmpty(entry.key)) data.ints[entry.key] = entry.value;

            if (floats != null)
                foreach (var entry in floats)
                    if (!string.IsNullOrEmpty(entry.key)) data.floats[entry.key] = entry.value;

            if (strings != null)
                foreach (var entry in strings)
                    if (!string.IsNullOrEmpty(entry.key)) data.strings[entry.key] = entry.value;

            if (completedLevels != null)
                data.completedLevels.AddRange(completedLevels);

            return data;
        }
    }

    [Serializable]
    private sealed class IntEntry
    {
        public string key;
        public int value;
    }

    [Serializable]
    private sealed class FloatEntry
    {
        public string key;
        public float value;
    }

    [Serializable]
    private sealed class StringEntry
    {
        public string key;
        public string value;
    }
}

/// <summary>
/// 游戏存档数据结构——可热更扩展字段。
/// </summary>
[Serializable]
public class GameSaveData
{
    /// <summary>整数型存档（金币、关卡进度等）</summary>
    public Dictionary<string, int> ints = new Dictionary<string, int>();

    /// <summary>浮点型存档</summary>
    public Dictionary<string, float> floats = new Dictionary<string, float>();

    /// <summary>字符串型存档</summary>
    public Dictionary<string, string> strings = new Dictionary<string, string>();

    /// <summary>已完成关卡列表</summary>
    public List<string> completedLevels = new List<string>();
}
