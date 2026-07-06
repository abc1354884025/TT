using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 一键生成 PreparePanel 所需的 Prefab：商店 Cell、物品栏 Cell、PreparePanel 本身。
/// 菜单：Tools → Create PreparePanel Prefabs
/// </summary>
public class PreparePanelPrefabCreator : EditorWindow
{
    [MenuItem("Tools/Create PreparePanel Prefabs")]
    public static void CreateAll()
    {
        CreateShopCellPrefab();
        CreateInventoryCellPrefab();
        CreatePreparePanelPrefab();
        AssetDatabase.Refresh();
        Debug.Log("[Editor] PreparePanel 相关 Prefab 生成完毕！");
    }

    static void CreateShopCellPrefab()
    {
        // 商店 Cell：一个带 ShopItemWidget 的按钮
        var go = new GameObject("ShopCell");
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 80);

        // 背景
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.2f);

        // 名称文本
        var nameGo = new GameObject("NameText", typeof(TMP_Text));
        nameGo.transform.SetParent(go.transform, false);
        var nameTxt = nameGo.GetComponent<TMP_Text>();
        nameTxt.text = "Item Name";
        nameTxt.fontSize = 16;
        nameTxt.alignment = TextAlignmentOptions.MidLeft;
        var nameRt = nameGo.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 0); nameRt.anchorMax = new Vector2(1, 0.5f);
        nameRt.offsetMin = new Vector2(8, 0); nameRt.offsetMax = new Vector2(-8, 0);

        // 价格文本
        var priceGo = new GameObject("PriceText", typeof(TMP_Text));
        priceGo.transform.SetParent(go.transform, false);
        var priceTxt = priceGo.GetComponent<TMP_Text>();
        priceTxt.text = "10G";
        priceTxt.fontSize = 14;
        priceTxt.color = Color.yellow;
        priceTxt.alignment = TextAlignmentOptions.MidRight;
        var priceRt = priceGo.GetComponent<RectTransform>();
        priceRt.anchorMin = new Vector2(0, 0.5f); priceRt.anchorMax = new Vector2(1, 1);
        priceRt.offsetMin = new Vector2(8, 0); priceRt.offsetMax = new Vector2(-8, 0);

        // Button
        var btn = go.AddComponent<Button>();

        // ShopItemWidget
        go.AddComponent<ShopItemWidget>();

        SavePrefab(go, "Assets/Prefabs/UI/Widgets/ShopCell.prefab");
    }

    static void CreateInventoryCellPrefab()
    {
        // 物品栏 Cell：一个带文本的按钮
        var go = new GameObject("InventoryCell");
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160, 60);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.3f);

        var nameGo = new GameObject("NameText", typeof(TMP_Text));
        nameGo.transform.SetParent(go.transform, false);
        var nameTxt = nameGo.GetComponent<TMP_Text>();
        nameTxt.text = "Item";
        nameTxt.fontSize = 16;
        nameTxt.alignment = TextAlignmentOptions.Center;
        var nameRt = nameGo.GetComponent<RectTransform>();
        nameRt.anchorMin = Vector2.zero; nameRt.anchorMax = Vector2.one;
        nameRt.offsetMin = new Vector2(4, 0); nameRt.offsetMax = new Vector2(-4, 0);

        go.AddComponent<Button>();

        SavePrefab(go, "Assets/Prefabs/UI/Widgets/InventoryCell.prefab");
    }

    static void CreatePreparePanelPrefab()
    {
        var go = new GameObject("PreparePanel");
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        go.AddComponent<PreparePanel>();
        // 具体的 UI 布局需要在 Unity Editor 中手动拖入子对象
        // 此脚本只生成占位 Prefab，子对象（BackpackGridWidget 等）需手动创建

        SavePrefab(go, "Assets/Resources/UI/Panels/PreparePanel.prefab");
    }

    static void SavePrefab(GameObject go, string path)
    {
        // 确保目录存在
        var dir = System.IO.Path.GetDirectoryName(path);
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);
        Debug.Log($"[Editor] 创建 Prefab: {path}");
    }
}
