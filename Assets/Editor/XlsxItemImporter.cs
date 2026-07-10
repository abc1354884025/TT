using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 零依赖 xlsx → items.json
///   items.xlsx  — 物品主表，Effects 列为逗号分隔的效果ID
///   effects.xlsx — 效果定义表
/// 菜单：Tools → Import Config from XLSX
///
/// items.xlsx 列：Id|Name|IconPath|Rarity|Type|Shape|ATK|DEF|HP|Crit|Desc|Sell|Buy|Effects
/// effects.xlsx 列：Id|Trigger|Interval|Condition|Action|Value|Target
/// </summary>
public class XlsxItemImporter : EditorWindow
{
    private const string ItemsXlsx = "Assets/Resources/Config/items.xlsx";
    private const string EffectsXlsx = "Assets/Resources/Config/effects.xlsx";
    private const string JsonOutput = "Assets/Resources/Config/items.json";
    private const string NsUri = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    [MenuItem("Tools/Create Config XLSX")]
    public static void CreateXlsx()
    {
        if (!EditorUtility.DisplayDialog("创建配置表", "将生成 items.xlsx 和 effects.xlsx（含示例数据）", "确定", "取消")) return;

        // effects.xlsx
        var effHeaders = new[] { "Id","Trigger","Interval","Condition","Action","Value","Target" };
        var effData = new[] {
            S("eff_heal_5s","Periodic","5","","heal","1","self"),
            S("eff_synergy_armor","Synergy","","","addDef","5","self"),
            S("eff_low_hp_rage","Conditional","","hp<30%","addAtk","10","self"),
            S("eff_heal_reduction","DebuffEnemy","","","healReduction","50","enemy"),
        };
        WriteXlsx(EffectsXlsx, effHeaders, effData);
        Debug.Log($"[XLSX] effects.xlsx 已创建（{effData.Length + 1} 行）");

        // items.xlsx
        var itemHeaders = new[] { "Id","Name","IconPath","Rarity","Type","ShapeMatrix","Attack","Defense","HP","CritChance","Description","SellPrice","BuyPrice","Effects" };
        var itemData = new[] {
            S("rusty_sword","铁剑","Icons/rusty_sword","0","0","1;1;1","5","0","0","5","普通铁剑","5","15",""),
            S("wooden_shield","木盾","Icons/wooden_shield","0","1","1,1;1,1","0","4","5","0","圆盾","5","15",""),
            S("dagger","匕首","Icons/dagger","0","0","1,0;0,1","6","0","0","15","短小精悍","8","20",""),
            S("health_potion","生命药水","Icons/health_potion","0","2","1","0","0","10","0","回血","3","10","eff_heal_5s"),
            S("broadsword","长剑","Icons/broadsword","1","0","1,1,1,0;0,0,0,1","10","0","0","10","L形长剑","15","35",""),
            S("magic_ring","魔法戒指","Icons/magic_ring","3","2","1,1;1,1","5","5","10","10","魔戒","35","80","eff_synergy_armor"),
        };
        WriteXlsx(ItemsXlsx, itemHeaders, itemData);
        Debug.Log($"[XLSX] items.xlsx 已创建（{itemData.Length + 1} 行）");

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", "items.xlsx + effects.xlsx 已生成", "好的");
    }

    [MenuItem("Tools/Import Config from XLSX")]
    public static void Import()
    {
        // 1. 读效果表
        var effMap = new Dictionary<string, string>();
        if (File.Exists(EffectsXlsx))
        {
            var ss = ReadStrings(EffectsXlsx);
            var rows = ReadSheet(EffectsXlsx, ss);
            for (int i = 1; i < rows.Count; i++)
            {
                var r = rows[i];
                var id = V(r, 0); if (string.IsNullOrEmpty(id)) continue;
                var trigger = V(r, 1); var interval = V(r, 2);
                var cond = V(r, 3); var action = V(r, 4);
                var value = V(r, 5); var target = V(r, 6);

                var ps = new List<string> { $"\"trigger\":\"{trigger}\"", $"\"action\":\"{action}\"", $"\"value\":{value}" };
                if (!string.IsNullOrEmpty(interval) && interval != "0") ps.Add($"\"interval\":{interval}");
                if (!string.IsNullOrEmpty(cond)) ps.Add($"\"condition\":\"{E(cond)}\"");
                if (!string.IsNullOrEmpty(target)) ps.Add($"\"target\":\"{target}\"");
                effMap[id] = "{" + string.Join(",", ps) + "}";
            }
        }
        Debug.Log($"[XLSX] 效果: {effMap.Count} 条");

        // 2. 读物品表 → 输出 JSON
        if (!File.Exists(ItemsXlsx)) { Debug.LogError($"找不到 {ItemsXlsx}"); return; }
        var itemSs = ReadStrings(ItemsXlsx);
        var itemRows = ReadSheet(ItemsXlsx, itemSs);

        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine("  \"items\": [");

        int count = 0;
        for (int i = 1; i < itemRows.Count; i++)
        {
            var r = itemRows[i];
            if (r.Count < 10 || string.IsNullOrWhiteSpace(r[0])) continue;

            var id = V(r, 0);
            var effIds = (r.Count > 13 ? V(r, 13) : "").Split(',', StringSplitOptions.RemoveEmptyEntries);
            var effs = new List<string>();
            foreach (var eid in effIds)
                if (effMap.TryGetValue(eid.Trim(), out var ej)) effs.Add(ej);

            if (count > 0) sb.Append(",\n");
            sb.Append("    {");
            sb.Append($"\"Id\":\"{id}\",\"Name\":\"{E(V(r, 1))}\",\"IconPath\":\"{E(V(r, 2))}\",");
            sb.Append($"\"Rarity\":{V(r, 3)},\"Type\":{V(r, 4)},");
            sb.Append($"\"ShapeMatrix\":[{M(V(r, 5))}],");
            sb.Append($"\"Attack\":{V(r, 6)},\"Defense\":{V(r, 7)},\"HP\":{V(r, 8)},\"CritChance\":{V(r, 9)},");
            sb.Append($"\"Description\":\"{E(V(r, 10))}\",\"SellPrice\":{V(r, 11)},\"BuyPrice\":{V(r, 12)},");
            sb.Append($"\"Effects\":[{string.Join(",", effs)}]");
            sb.Append("}");
            count++;
        }

        sb.AppendLine("\n  ]\n}");
        File.WriteAllText(JsonOutput, sb.ToString());
        AssetDatabase.Refresh();
        Debug.Log($"[XLSX] {count} 物品 → {JsonOutput}");
        EditorUtility.DisplayDialog("完成", $"{count} 物品 + {effMap.Count} 效果", "好的");
    }

    // ====== 读取 xlsx ======

    static List<string> ReadStrings(string path)
    {
        var list = new List<string>();
        using var zip = ZipFile.OpenRead(path);
        var e = zip.GetEntry("xl/sharedStrings.xml");
        if (e == null) return list;
        using var r = XmlReader.Create(e.Open());
        while (r.Read())
            if (r.NodeType == XmlNodeType.Element && r.Name == "t")
                list.Add(r.ReadElementContentAsString());
        return list;
    }

    static List<List<string>> ReadSheet(string path, List<string> ss)
    {
        var result = new List<List<string>>();
        using var zip = ZipFile.OpenRead(path);
        var e = zip.GetEntry("xl/worksheets/sheet1.xml");
        if (e == null) return result;
        var doc = new XmlDocument();
        doc.Load(e.Open());
        var n = new XmlNamespaceManager(doc.NameTable);
        n.AddNamespace("s", NsUri);

        foreach (XmlNode r in doc.SelectNodes("//s:row", n))
        {
            int max = -1;
            var m = new Dictionary<int, string>();
            foreach (XmlNode c in r.ChildNodes)
            {
                if (c.Name != "c") continue;
                int col = CI(c.Attributes?["r"]?.Value);
                if (col < 0) continue;
                var t = c.Attributes?["t"]?.Value;
                var v = (c.SelectSingleNode("s:v", n))?.InnerText ?? "";
                m[col] = (t == "s" && int.TryParse(v, out int i) && i < ss.Count) ? ss[i] : v;
                if (col > max) max = col;
            }
            var row = new List<string>();
            for (int c = 0; c <= max; c++) row.Add(m.TryGetValue(c, out var x) ? x : "");
            result.Add(row);
        }
        return result;
    }

    static int CI(string r) { if (string.IsNullOrEmpty(r)) return -1; int x = 0; foreach (char c in r) { if (char.IsLetter(c)) x = x * 26 + (char.ToUpper(c) - 'A' + 1); else break; } return x - 1; }

    // ====== 辅助 ======

    static string V(List<string> r, int i) => i < r.Count ? r[i].Trim() : "0";
    static string E(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    static string M(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "1";
        var p = new List<string>();
        foreach (var row in raw.Split(';')) p.Add(string.Join(",", row.Split(',')));
        return string.Join("],[", p);
    }

    static string[] S(params string[] v) => v;

    static void WriteXlsx(string path, string[] headers, string[][] rows)
    {
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        var ss = new List<string>();
        int SS(string s) { int i = ss.IndexOf(s); if (i >= 0) return i; ss.Add(s); return ss.Count - 1; }

        var xml = new StringBuilder();
        xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        xml.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");

        for (int r = -1; r < rows.Length; r++)
        {
            xml.Append($"<row r=\"{r + 2}\">");
            var cols = r == -1 ? headers : rows[r];
            bool isItem = cols.Length > 10; // items 表需要区分数字/字符串列
            for (int c = 0; c < cols.Length; c++)
            {
                var val = cols[c];
                var refStr = (char)('A' + c % 26) + "" + (r + 2); // 简单列引用（不超过 Z）
                if (isItem && r >= 0 && c >= 3 && c <= 9)
                    xml.Append($"<c r=\"{refStr}\"><v>{val}</v></c>");
                else
                    xml.Append($"<c r=\"{refStr}\" t=\"s\"><v>{SS(val)}</v></c>");
            }
            xml.Append("</row>");
        }
        xml.Append("</sheetData></worksheet>");
        var sw = new StreamWriter(zip.CreateEntry("xl/worksheets/sheet1.xml").Open()); sw.Write(xml.ToString()); sw.Dispose();

        // meta
        void A(string p, string c) { var w = new StreamWriter(zip.CreateEntry(p).Open()); w.Write(c); w.Dispose(); }
        A("[Content_Types].xml", "<?xml version=\"1.0\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"xml\" ContentType=\"application/xml\"/><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/sharedStrings.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml\"/></Types>");
        A("_rels/.rels", "<?xml version=\"1.0\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
        A("xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings\" Target=\"sharedStrings.xml\"/></Relationships>");
        A("xl/workbook.xml", "<?xml version=\"1.0\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");

        var sXml = new StringBuilder();
        sXml.Append("<?xml version=\"1.0\"?>");
        sXml.Append($"<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"{ss.Count}\" uniqueCount=\"{ss.Count}\">");
        foreach (var s in ss) sXml.Append($"<si><t xml:space=\"preserve\">{System.Security.SecurityElement.Escape(s)}</t></si>");
        sXml.Append("</sst>");
        A("xl/sharedStrings.xml", sXml.ToString());
    }
}
