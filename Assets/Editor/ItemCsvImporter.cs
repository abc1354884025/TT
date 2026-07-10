using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Excel 导出的 CSV → items.json 转换器。
/// 菜单：Tools → Import Items from CSV
///
/// CSV 格式：
///   Id,Name,IconPath,Rarity(0-4),Type(0-2),ShapeMatrix,Attack,Defense,HP,CritChance,Description,SellPrice,BuyPrice,Effects
///
/// ShapeMatrix 格式："1,1;1,0"  (分号分行，逗号分列)
/// Effects 格式：[{"trigger":"Periodic","interval":5,"action":"heal","value":1}]
/// </summary>
public class ItemCsvImporter : EditorWindow
{
    private const string CsvPath = "Assets/Resources/Config/items.csv";
    private const string JsonOutputPath = "Assets/Resources/Config/items.json";

    [MenuItem("Tools/Import Items from CSV")]
    public static void Import()
    {
        if (!File.Exists(CsvPath))
        {
            Debug.LogError($"[CSV] 找不到 {CsvPath}");
            return;
        }

        var items = new List<ItemData>();
        var lines = File.ReadAllLines(CsvPath);

        if (lines.Length < 2) { Debug.LogError("[CSV] CSV 为空"); return; }

        for (int i = 1; i < lines.Length; i++)
        {
            var fields = ParseCsvLine(lines[i]);
            if (fields.Length < 13) continue;

            try
            {
                var item = new ItemData
                {
                    Id = fields[0].Trim(),
                    Name = fields[1].Trim(),
                    IconPath = fields[2].Trim(),
                    Rarity = (ItemRarity)int.Parse(fields[3]),
                    Type = (ItemType)int.Parse(fields[4]),
                    ShapeMatrix = ParseShape(fields[5]),
                    Attack = int.Parse(fields[6]),
                    Defense = int.Parse(fields[7]),
                    HP = int.Parse(fields[8]),
                    CritChance = int.Parse(fields[9]),
                    Description = fields[10].Trim(),
                    SellPrice = int.Parse(fields[11]),
                    BuyPrice = int.Parse(fields[12]),
                    Effects = ParseEffects(fields.Length > 13 ? fields[13] : ""),
                };
                items.Add(item);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CSV] 第 {i + 1} 行解析失败: {e.Message}\n{lines[i]}");
            }
        }

        // 写入 JSON，Pretty Print
        var json = JsonUtility.ToJson(new ItemListWrapper { items = items.ToArray() }, true);
        File.WriteAllText(JsonOutputPath, json);
        AssetDatabase.Refresh();

        Debug.Log($"[CSV] 导入完成！{items.Count} 个物品 → {JsonOutputPath}");
        EditorUtility.DisplayDialog("导入完成", $"{items.Count} 个物品已导出到\n{JsonOutputPath}", "好的");
    }

    // ====== CSV 解析 ======

    /// <summary>解析 CSV 行（支持引号包围的字段）</summary>
    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        string current = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"') { inQuotes = !inQuotes; continue; }
            if (c == ',' && !inQuotes) { result.Add(current); current = ""; continue; }
            current += c;
        }
        result.Add(current);
        return result.ToArray();
    }

    /// <summary>解析形状矩阵 "1,1,0;0,1,1;0,1,0" → int[][]</summary>
    private static int[][] ParseShape(string raw)
    {
        raw = raw.Trim('"', ' ');
        if (string.IsNullOrEmpty(raw)) return new[] { new[] { 1 } };

        var rows = raw.Split(';');
        var result = new int[rows.Length][];
        for (int r = 0; r < rows.Length; r++)
        {
            var cols = rows[r].Split(',');
            result[r] = new int[cols.Length];
            for (int c = 0; c < cols.Length; c++)
                int.TryParse(cols[c].Trim(), out result[r][c]);
        }
        return result;
    }

    /// <summary>解析效果 JSON</summary>
    private static string ParseEffects(string raw)
    {
        raw = raw.Trim('"', ' ');
        return string.IsNullOrEmpty(raw) ? "[]" : raw;
    }

    [Serializable]
    private class ItemListWrapper
    {
        public ItemData[] items;
    }
}
