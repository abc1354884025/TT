using System;
using System.Collections.Generic;
using TTSDK;
using UnityEngine;

/// <summary>
/// 存档模块——封装 TT.Save / TT.LoadSaving，按游戏类型区分存档槽位。
/// 用法：
///   SaveManager.Load();           // 游戏启动时加载
///   SaveManager.Save();           // 关键节点存档
///   SaveManager.SetInt("gold", 100);
///   int gold = SaveManager.GetInt("gold");
/// </summary>
public static class SaveManager
{
    private const string SaveKey = "backpack_brawl_save";

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
            Data = TT.LoadSaving<GameSaveData>();
            if (Data == null)
            {
                Data = new GameSaveData();
                Debug.Log("[SaveManager] 无存档，创建新存档");
            }
            else
            {
                Debug.Log($"[SaveManager] 加载存档成功，金币: {GetInt("gold", 0)}");
            }
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
            bool ok = TT.Save(Data);
            Debug.Log($"[SaveManager] 保存{(ok ? "成功" : "失败")}");
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
            TT.DeleteSaving<GameSaveData>();
            Data = new GameSaveData();
            Debug.Log("[SaveManager] 存档已删除");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveManager] 删除异常: {e.Message}");
        }
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
