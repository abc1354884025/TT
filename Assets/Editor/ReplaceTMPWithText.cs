using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 一键将项目中所有 Prefab 的 TMP_Text / TextMeshProUGUI 替换为 UGUI Text。
/// 菜单：Tools → Replace TMP with Text in All Prefabs
/// </summary>
public class ReplaceTMPWithText : EditorWindow
{
    [MenuItem("Tools/Replace TMP with Text in All Prefabs")]
    public static void ReplaceAll()
    {
        if (!EditorUtility.DisplayDialog("确认替换",
            "将搜索所有 Prefab 中的 TMP_Text/TextMeshProUGUI 并替换为 UGUI Text。\n建议先备份！",
            "执行", "取消"))
            return;

        var guids = AssetDatabase.FindAssets("t:Prefab");
        int total = 0;
        int modified = 0;

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            bool changed = false;

            // 遍历 Prefab 中所有 TMP_Text 和 TextMeshProUGUI
            var allTmp = prefab.GetComponentsInChildren<TMP_Text>(true);
            var allTmpUgui = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);

            // 合并去重
            var allComponents = new HashSet<Component>();
            foreach (var t in allTmp) allComponents.Add(t);
            foreach (var t in allTmpUgui) allComponents.Add(t);

            total += allComponents.Count;

            foreach (var tmp in allComponents)
            {
                if (tmp == null) continue;
                var go = tmp.gameObject;

                // 保存属性
                string text = (tmp is TMP_Text t) ? t.text :
                              (tmp is TextMeshProUGUI u) ? u.text : "";

                float fontSize = (tmp is TMP_Text t2) ? t2.fontSize :
                                 (tmp is TextMeshProUGUI u2) ? u2.fontSize : 14;

                Color color = (tmp is TMP_Text t3) ? t3.color :
                              (tmp is TextMeshProUGUI u3) ? u3.color : Color.white;

                // 对齐方式转换
                TextAnchor alignment = TextAnchor.MiddleCenter;
                if (tmp is TMP_Text t4)
                {
                    alignment = t4.alignment switch
                    {
                        TextAlignmentOptions.TopLeft => TextAnchor.UpperLeft,
                        TextAlignmentOptions.Top => TextAnchor.UpperCenter,
                        TextAlignmentOptions.TopRight => TextAnchor.UpperRight,
                        TextAlignmentOptions.Left => TextAnchor.MiddleLeft,
                        TextAlignmentOptions.Center => TextAnchor.MiddleCenter,
                        TextAlignmentOptions.Right => TextAnchor.MiddleRight,
                        TextAlignmentOptions.BottomLeft => TextAnchor.LowerLeft,
                        TextAlignmentOptions.Bottom => TextAnchor.LowerCenter,
                        TextAlignmentOptions.BottomRight => TextAnchor.LowerRight,
                        _ => TextAnchor.MiddleCenter
                    };
                }

                // 删除 TMP 组件
                DestroyImmediate(tmp, true);

                // 添加 UGUI Text
                var uguiText = go.AddComponent<Text>();
                uguiText.text = text;
                uguiText.fontSize = Mathf.RoundToInt(fontSize);
                uguiText.color = color;
                uguiText.alignment = alignment;
                uguiText.raycastTarget = true;

                changed = true;
            }

            if (changed)
            {
                PrefabUtility.SavePrefabAsset(prefab);
                modified++;
                Debug.Log($"[TMP→Text] 替换完成: {path}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TMP→Text] 全部完成！处理 {modified} 个 Prefab，替换 {total} 个组件");
        EditorUtility.DisplayDialog("完成", $"处理 {modified} 个 Prefab\n替换 {total} 个 TMP 组件", "好的");
    }
}
