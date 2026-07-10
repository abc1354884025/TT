using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 零依赖 xlsx → items.json 转换器。不引用 HotUpdate 类型，纯字符串拼接。
/// 菜单：Tools → Import Items from XLSX
///
/// xlsx 表头（第1行）：
///   Id | Name | IconPath | Rarity | Type | ShapeMatrix | Attack | Defense | HP | CritChance | Description | SellPrice | BuyPrice | Effects
///
/// ShapeMatrix 格式："1,1;1,0"（分号分行，逗号分列）
/// Effects 格式：[{"trigger":"Periodic","interval":5,"action":"heal","value":1}]
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
            Debug.LogError($"[XLSX] 找不到 {XlsxPath}。请将 items.xlsx 放到此路径。");
            return;
        }

        List<string> sharedStrings;
        List<List<string>> rows;

        using (var zip = ZipFile.OpenRead(XlsxPath))
        {
            sharedStrings = ParseSharedStrings(zip);
            rows = ParseSheet(zip, sharedStrings);
        }

        if (rows.Count < 2) { Debug.LogError("[XLSX] 无数据行"); return; }

        var sb = new StringBuilder();
        sb.AppendLine("{\n  \"items\": [");

        int count = 0;
        for (int i = 1; i < rows.Count; i++)
        {
            var r = rows[i];
            if (r.Count < 6 || string.IsNullOrWhiteSpace(r[0])) continue;

            try
            {
                var id = Val(r, 0); var name = Val(r, 1); var icon = Val(r, 2);
                var rarity = Val(r, 3); var type = Val(r, 4); var shape = Val(r, 5);
                var atk = Val(r, 6); var def = Val(r, 7); var hp = Val(r, 8);
                var crit = Val(r, 9); var desc = Val(r, 10); var sell = Val(r, 11);
                var buy = Val(r, 12); var effects = r.Count > 13 ? Val(r, 13) : "[]";

                if (string.IsNullOrEmpty(effects) || effects == "0") effects = "[]";

                sb.Append("    {");
                sb.Append($"\"Id\":\"{id}\",\"Name\":\"{name}\",\"IconPath\":\"{icon}\",");
                sb.Append($"\"Rarity\":{rarity},\"Type\":{type},");
                sb.Append($"\"ShapeMatrix\":[{ToMatrix(shape)}],");
                sb.Append($"\"Attack\":{atk},\"Defense\":{def},\"HP\":{hp},\"CritChance\":{crit},");
                sb.Append($"\"Description\":\"{Escape(desc)}\",");
                sb.Append($"\"SellPrice\":{sell},\"BuyPrice\":{buy},");
                sb.Append($"\"Effects\":{effects}");
                sb.Append("}");

                if (i < rows.Count - 1) sb.Append(",");
                sb.AppendLine();
                count++;
            }
            catch (Exception e)
            {
                Debug.LogError($"[XLSX] 第 {i + 1} 行出错: {e.Message}");
            }
        }

        sb.AppendLine("  ]\n}");

        File.WriteAllText(JsonOutputPath, sb.ToString());
        AssetDatabase.Refresh();

        Debug.Log($"[XLSX] 完成！{count} 个物品 → {JsonOutputPath}");
        EditorUtility.DisplayDialog("导入完成", $"{count} 个物品已导出", "好的");
    }

    // ====== XLSX 解析 ======

    private static List<string> ParseSharedStrings(ZipArchive zip)
    {
        var list = new List<string>();
        var entry = zip.GetEntry("xl/sharedStrings.xml");
        if (entry == null) return list;

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

    private static List<List<string>> ParseSheet(ZipArchive zip, List<string> ss)
    {
        var result = new List<List<string>>();
        var entry = zip.GetEntry("xl/worksheets/sheet1.xml");
        if (entry == null) return result;

        using var stream = entry.Open();
        var doc = new XmlDocument();
        doc.Load(stream);

        foreach (XmlNode rowNode in doc.GetElementsByTagName("row"))
        {
            var row = new List<string>();
            foreach (XmlNode cell in rowNode.ChildNodes)
            {
                if (cell.Name != "c") continue;
                var t = cell.Attributes?["t"]?.Value;
                var vNode = cell.SelectSingleNode("v");
                var v = vNode?.InnerText ?? "";

                if (t == "s" && int.TryParse(v, out int idx) && idx < ss.Count)
                    row.Add(ss[idx]);
                else
                    row.Add(v);
            }
            result.Add(row);
        }
        return result;
    }

    // ====== 辅助 ======

    private static string Val(List<string> row, int idx)
        => idx < row.Count ? row[idx].Trim() : "0";

    private static string Escape(string s)
        => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");

    /// <summary>"1,1;1,0" → [1,1],[1,0]</summary>
    private static string ToMatrix(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "1";
        var rows = raw.Split(';');
        var parts = new List<string>();
        foreach (var row in rows)
        {
            var nums = row.Split(',');
            parts.Add(string.Join(",", nums));
        }
        return string.Join("],[", parts);
    }
}
