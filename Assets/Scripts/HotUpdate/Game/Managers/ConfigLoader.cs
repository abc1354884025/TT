using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 配置加载器。从 Resources 或 CDN 字节加载 JSON 配置。
/// CDN 热更时，调用 LoadFromBytes 覆盖本地配置。
/// </summary>
public static class ConfigLoader
{
    private static BalanceConfig _balance;
    private static EnemyConfig _enemyConfig;
    private static EnemyEntry[] _enemies;

    public static BalanceConfig Balance => _balance;
    public static EnemyEntry[] Enemies => _enemies;

    /// <summary>一次性加载所有配置</summary>
    public static void LoadAll()
    {
        LoadBalance();
        LoadItems();
        LoadEnemies();
    }

    /// <summary>加载平衡配置</summary>
    public static void LoadBalance(byte[] overrideBytes = null)
    {
        string json = null;
        if (overrideBytes != null)
            json = System.Text.Encoding.UTF8.GetString(overrideBytes);
        else
        {
            var asset = Resources.Load<TextAsset>("Config/balance");
            json = asset?.text;
        }

        if (!string.IsNullOrEmpty(json))
        {
            _balance = JsonUtility.FromJson<BalanceConfig>(json);
            Debug.Log($"[ConfigLoader] 加载 balance，格子: {_balance.gridWidth}x{_balance.gridHeight}");
        }
        else
        {
            _balance = new BalanceConfig(); // 默认值
            Debug.LogWarning("[ConfigLoader] balance.json 未找到，使用默认值");
        }
    }

    /// <summary>加载物品数据库（当前版本硬编码，后续热更可在此处从 CDN JSON 覆盖）</summary>
    public static void LoadItems(byte[] overrideBytes = null)
    {
        string json = overrideBytes != null
            ? System.Text.Encoding.UTF8.GetString(overrideBytes)
            : Resources.Load<TextAsset>("Config/items")?.text;

        if (!ItemDatabase.ReloadFromJson(json))
            Debug.LogWarning($"[ConfigLoader] items.json 无效或为空，保留内置物品 ({ItemDatabase.Count} 个)");
    }

    /// <summary>加载敌人配置</summary>
    public static void LoadEnemies(byte[] overrideBytes = null)
    {
        string json = null;
        if (overrideBytes != null)
            json = System.Text.Encoding.UTF8.GetString(overrideBytes);
        else
        {
            var asset = Resources.Load<TextAsset>("Config/enemies");
            json = asset?.text;
        }

        if (!string.IsNullOrEmpty(json))
        {
            var cfg = JsonUtility.FromJson<EnemyConfig>(json);
            _enemyConfig = cfg;
            _enemies = cfg?.enemies ?? Array.Empty<EnemyEntry>();
            Debug.Log($"[ConfigLoader] 加载 {_enemies.Length} 个敌人");
        }
        else
        {
            _enemies = Array.Empty<EnemyEntry>();
            Debug.LogWarning("[ConfigLoader] enemies.json 未找到");
        }
    }

    /// <summary>根据难度获取敌人</summary>
    public static EnemyEntry GetEnemyByDifficulty(int difficulty)
    {
        if (_enemies == null) return null;
        // 找最接近的难度
        EnemyEntry best = null;
        foreach (var e in _enemies)
        {
            if (e.difficulty <= difficulty)
                best = e;
        }
        return best ?? (_enemies.Length > 0 ? _enemies[0] : null);
    }

    /// <summary>网格配置快捷访问</summary>
    public static int GridWidth => _balance?.gridWidth ?? 6;
    public static int GridHeight => _balance?.gridHeight ?? 8;
}
