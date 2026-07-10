using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 直接读取 .xlsx 文件导出为 items.json（无第三方依赖）。
/// 菜单：Tools → Import Items from XLSX
/// </summary>
public class XlsxItemImporter : EditorWindow
{
    private const string XlsxPath = "Assets/Resources/Config/items.xlsx";
    private const string JsonOutputPath = "Assets/Resources/Config/items.json";

    [MenuItem("Tools/Import Items from XLSX")]
    public static void Import()
    {
        if (!File.Exists(XlsxPath))
        {
            Debug.LogError($"[XLSX] 找不到 {XlsxPath}，请在 Excel 中另存为 items.xlsx 放到此路径");
            return;
        }

        // 解析 xlsx（本质是 zip 包）
        List<string> sharedStrings;
        List<List<string>> rows;

        using (var zip = ZipFile.OpenRead(XlsxPath))
        {
            sharedStrings = ParseSharedStrings(zip);
            rows = ParseSheet(zip, sharedStrings);
        }

        // 第一行是表头，跳过
        var items = new List<ItemData>();
        for (int i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Count < 13 || string.IsNullOrWhiteSpace(row[0])) continue;

            try
            {
                items.Add(new ItemData
                {
                    Id = row[0].Trim(),
                    Name = row[1].Trim(),
                    IconPath = row[2].Trim(),
                    Rarity = (ItemRarity)int.Parse(row[3]),
                    Type = (ItemType)int.Parse(row[4]),
                    ShapeMatrix = ParseShape(row[5]),
                    Attack = int.Parse(row[6]),
                    Defense = int.Parse(row[7]),
                    HP = int.Parse(row[8]),
                    CritChance = int.Parse(row[9]),
                    Description = row[10].Trim(),
                    SellPrice = int.Parse(row[11]),
                    BuyPrice = int.Parse(row[12]),
                    Effects = row.Count > 13 ? row[13].Trim() : "[]",
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[XLSX] 第 {i + 1} 行解析失败: {e.Message}");
            }
        }

        var json = JsonUtility.ToJson(new ItemListWrapper { items = items.ToArray() }, true);
        File.WriteAllText(JsonOutputPath, json);
        AssetDatabase.Refresh();

        Debug.Log($"[XLSX] 导入完成！{items.Count} 个物品 → {JsonOutputPath}");
        EditorUtility.DisplayDialog("导入完成", $"{items.Count} 个物品已导出到\n{JsonOutputPath}", "好的");
    }

    // ====== XLSX 解析 ======

    private static List<string> ParseSharedStrings(ZipArchive zip)
    {
        var entry = zip.GetEntry("xl/sharedStrings.xml");
        if (entry == null) return new List<string>();

        var list = new List<string>();
        using var stream = entry.Open();
        using var reader = XmlReader.Create(stream);
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "t")
            {
                reader.Read();
                list.Add(reader.Value);
            }
        }
        return list;
    }

    private static List<List<string>> ParseSheet(ZipArchive zip, List<string> sharedStrings)
    {
        var result = new List<List<string>>();
        var entry = zip.GetEntry("xl/worksheets/sheet1.xml");
        if (entry == null) return result;

        using var stream = entry.Open();
        var doc = new XmlDocument();
        doc.Load(stream);

        var rows = doc.GetElementsByTagName("row");
        foreach (XmlNode rowNode in rows)
        {
            var row = new List<string>();
            var cells = rowNode.ChildNodes;
            foreach (XmlNode cell in cells)
            {
                if (cell.Name != "c") continue;
                var t = cell.Attributes?["t"]?.Value;     // s = shared string
                var vNode = cell.SelectSingleNode("v");
                var v = vNode?.InnerText ?? "";

                if (t == "s" && int.TryParse(v, out int idx) && idx < sharedStrings.Count)
                    row.Add(sharedStrings[idx]);
                else
                    row.Add(v);
            }
            result.Add(row);
        }
        return result;
    }

    // ====== 形状解析 ======

    private static int[][] ParseShape(string raw)
    {
        raw = raw.Trim();
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

    [Serializable]
    private class ItemListWrapper { public ItemData[] items; }
}
