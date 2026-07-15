using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TTSDK.UNBridgeLib.LitJson;

/// <summary>
/// 物品配置编辑器。只编辑 JSON DTO，因此不依赖 HotUpdate 程序集；保存后由 ConfigLoader 读取同一份 items.json。
/// </summary>
public class EquipmentEditorWindow : EditorWindow
{
    private const string ConfigAssetPath = "Assets/Resources/Config/items.json";
    private const int ShapeSize = 6;
    private static readonly string[] RarityNames = { "Common", "Uncommon", "Rare", "Epic", "Legendary" };
    private static readonly string[] TypeNames = { "Weapon", "Armor", "Trinket" };
    private static readonly string[] PlacementTypeNames = { "Equipment", "BackpackItem" };

    [Serializable]
    private class EquipmentConfigFile { public List<EquipmentRecord> items = new List<EquipmentRecord>(); }

    [Serializable]
    private class EquipmentRecord
    {
        public string Id = "new_equipment";
        public string Name = "新装备";
        public string IconPath;
        public string BackpackVisualPath;
        public string BackpackAnimationState;
        public string[] EffectIds = Array.Empty<string>();
        public int Rarity;
        public int Type;
        public int PlacementType;
        public int[][] ShapeMatrix = { new[] { 1 } };
        public int Attack;
        public int Defense;
        public int HP;
        public int CritChance;
        public string Description;
        public int SellPrice;
        public int BuyPrice;
    }

    private EquipmentConfigFile _config;
    private Vector2 _listScroll;
    private Vector2 _detailScroll;
    private int _selectedIndex = -1;
    private bool _dirty;
    private string _previewKey;
    private Sprite _draggedIcon;
    private GameObject _draggedVisual;

    [MenuItem("Tools/装备编辑器")]
    public static void Open()
    {
        var window = GetWindow<EquipmentEditorWindow>("装备编辑器");
        window.minSize = new Vector2(900, 520);
        window.LoadConfig();
    }

    private void OnEnable() => LoadConfig();

    private void OnGUI()
    {
        if (_config == null) LoadConfig();

        DrawToolbar();
        EditorGUILayout.BeginHorizontal();
        DrawList();
        DrawDetails();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label(_dirty ? "未保存修改" : "已保存", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("重新读取", EditorStyles.toolbarButton, GUILayout.Width(70)))
        {
            if (!_dirty || EditorUtility.DisplayDialog("丢弃修改", "重新读取会丢弃未保存内容。", "继续", "取消"))
                LoadConfig();
        }
        if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(60))) SaveConfig();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawList()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(245));
        EditorGUILayout.LabelField("装备列表", EditorStyles.boldLabel);
        _listScroll = EditorGUILayout.BeginScrollView(_listScroll, "box");
        for (var i = 0; i < _config.items.Count; i++)
        {
            var item = _config.items[i];
            var label = $"{item.Name}  ({item.Id})";
            if (GUILayout.Toggle(i == _selectedIndex, label, "Button")) _selectedIndex = i;
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("新增"))
        {
            _config.items.Add(new EquipmentRecord { Id = GetUniqueId("new_equipment") });
            _selectedIndex = _config.items.Count - 1;
            _dirty = true;
        }
        using (new EditorGUI.DisabledScope(_selectedIndex < 0))
        {
            if (GUILayout.Button("复制"))
            {
                var copy = JsonUtility.FromJson<EquipmentRecord>(JsonUtility.ToJson(_config.items[_selectedIndex]));
                copy.Id = GetUniqueId(copy.Id + "_copy");
                copy.Name += " 副本";
                _config.items.Add(copy);
                _selectedIndex = _config.items.Count - 1;
                _dirty = true;
            }
            if (GUILayout.Button("删除"))
            {
                _config.items.RemoveAt(_selectedIndex);
                _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _config.items.Count - 1);
                _dirty = true;
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawDetails()
    {
        EditorGUILayout.BeginVertical("box");
        if (_selectedIndex < 0 || _selectedIndex >= _config.items.Count)
        {
            EditorGUILayout.HelpBox("请选择或新增一件装备。", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        var item = _config.items[_selectedIndex];
        EditorGUI.BeginChangeCheck();
        _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
        EditorGUILayout.LabelField("基础数据", EditorStyles.boldLabel);
        item.Id = EditorGUILayout.TextField("ID", item.Id);
        item.Name = EditorGUILayout.TextField("名称", item.Name);
        item.Rarity = EditorGUILayout.Popup("稀有度", item.Rarity, RarityNames);
        item.Type = EditorGUILayout.Popup("类型", item.Type, TypeNames);
        item.PlacementType = EditorGUILayout.Popup("用途", item.PlacementType, PlacementTypeNames);
        item.Description = EditorGUILayout.TextField("描述", item.Description);
        item.Attack = EditorGUILayout.IntField("攻击", item.Attack);
        item.Defense = EditorGUILayout.IntField("防御", item.Defense);
        item.HP = EditorGUILayout.IntField("生命", item.HP);
        item.CritChance = EditorGUILayout.IntSlider("暴击率", item.CritChance, 0, 100);
        item.BuyPrice = EditorGUILayout.IntField("买入价", item.BuyPrice);
        item.SellPrice = EditorGUILayout.IntField("卖出价", item.SellPrice);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("背包视觉", EditorStyles.boldLabel);
        DrawResourceFields(item);
        item.BackpackAnimationState = EditorGUILayout.TextField("Animator 状态", item.BackpackAnimationState);
        EditorGUILayout.HelpBox("路径相对于 Resources，例如 Equipment/fire_sword/backpack。Prefab 缺失时会回退到图标。", MessageType.None);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("技能", EditorStyles.boldLabel);
        var effects = string.Join(",", item.EffectIds ?? Array.Empty<string>());
        effects = EditorGUILayout.TextField("效果 ID（逗号分隔）", effects);
        item.EffectIds = effects.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("占格形状", EditorStyles.boldLabel);
        DrawShape(item);
        EditorGUILayout.EndScrollView();

        if (EditorGUI.EndChangeCheck()) _dirty = true;
        EditorGUILayout.EndVertical();
    }

    private void DrawShape(EquipmentRecord item)
    {
        var shape = ToSquareShape(item.ShapeMatrix);
        for (var y = 0; y < ShapeSize; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (var x = 0; x < ShapeSize; x++)
            {
                var enabled = shape[x, y] != 0;
                var next = GUILayout.Toggle(enabled, GUIContent.none, "Button", GUILayout.Width(28), GUILayout.Height(24));
                shape[x, y] = next ? 1 : 0;
            }
            EditorGUILayout.EndHorizontal();
        }
        item.ShapeMatrix = TrimShape(shape);
    }

    private void DrawResourceFields(EquipmentRecord item)
    {
        SyncVisualReferences(item);
        var currentIcon = _draggedIcon;
        var nextIcon = (Sprite)EditorGUILayout.ObjectField("图标（可拖拽）", currentIcon, typeof(Sprite), false);
        if (nextIcon != currentIcon)
        {
            _draggedIcon = nextIcon;
            item.IconPath = GetResourcesPath(nextIcon);
        }

        var currentVisual = _draggedVisual;
        var nextVisual = (GameObject)EditorGUILayout.ObjectField("视觉 Prefab（可拖拽）", currentVisual, typeof(GameObject), false);
        if (nextVisual != currentVisual)
        {
            _draggedVisual = nextVisual;
            item.BackpackVisualPath = GetResourcesPath(nextVisual);
        }
    }

    private void SyncVisualReferences(EquipmentRecord item)
    {
        var key = item.Id + "|" + item.IconPath + "|" + item.BackpackVisualPath;
        if (_previewKey == key) return;
        _previewKey = key;
        _draggedIcon = LoadResourceAsset<Sprite>(item.IconPath);
        _draggedVisual = LoadResourceAsset<GameObject>(item.BackpackVisualPath);
    }

    private static T LoadResourceAsset<T>(string resourcePath) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(resourcePath)) return null;
        var assetPath = "Assets/Resources/" + resourcePath;
        var direct = AssetDatabase.LoadAssetAtPath<T>(assetPath)
            ?? AssetDatabase.LoadAssetAtPath<T>(assetPath + ".prefab");
        if (direct != null) return direct;

        var resourcePathWithoutExtension = resourcePath.Replace('\\', '/');
        var fileName = Path.GetFileNameWithoutExtension(resourcePathWithoutExtension);
        foreach (var guid in AssetDatabase.FindAssets(fileName))
        {
            var candidatePath = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
            const string resourcesPrefix = "Assets/Resources/";
            if (!candidatePath.StartsWith(resourcesPrefix, StringComparison.Ordinal)) continue;
            var candidateResourcePath = Path.ChangeExtension(candidatePath.Substring(resourcesPrefix.Length), null).Replace('\\', '/');
            if (!string.Equals(candidateResourcePath, resourcePathWithoutExtension, StringComparison.OrdinalIgnoreCase)) continue;

            var asset = AssetDatabase.LoadAssetAtPath<T>(candidatePath);
            if (asset != null) return asset;
            foreach (var subAsset in AssetDatabase.LoadAllAssetsAtPath(candidatePath))
                if (subAsset is T typedAsset) return typedAsset;
        }
        return null;
    }

    private static string GetResourcesPath(UnityEngine.Object asset)
    {
        if (asset == null) return string.Empty;
        var assetPath = AssetDatabase.GetAssetPath(asset).Replace('\\', '/');
        const string resourcesPrefix = "Assets/Resources/";
        if (!assetPath.StartsWith(resourcesPrefix, StringComparison.Ordinal))
        {
            Debug.LogWarning("[EquipmentEditor] 资源必须位于 Assets/Resources 下，无法写入该引用。");
            return string.Empty;
        }
        return Path.ChangeExtension(assetPath.Substring(resourcesPrefix.Length), null).Replace('\\', '/');
    }

    private void LoadConfig()
    {
        var absolutePath = Path.GetFullPath(ConfigAssetPath);
        var restoredMissingShapes = false;
        try
        {
            if (File.Exists(absolutePath))
            {
                var json = File.ReadAllText(absolutePath);
                _config = JsonUtility.FromJson<EquipmentConfigFile>(json) ?? new EquipmentConfigFile();
                _config.items ??= new List<EquipmentRecord>();
                RestoreShapeMatrices(json);
                restoredMissingShapes = RestoreMissingShapes();
            }
            else
            {
                _config = new EquipmentConfigFile();
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"[EquipmentEditor] 读取配置失败: {exception.Message}");
            _config = new EquipmentConfigFile();
        }
        _config ??= new EquipmentConfigFile();
        _config.items ??= new List<EquipmentRecord>();
        _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _config.items.Count - 1);
        _dirty = restoredMissingShapes;
    }

    private void SaveConfig()
    {
        if (_config.items.Any(item => string.IsNullOrWhiteSpace(item.Id)))
        {
            EditorUtility.DisplayDialog("无法保存", "每件装备都需要 ID。", "确定");
            return;
        }
        if (_config.items.GroupBy(item => item.Id).Any(group => group.Count() > 1))
        {
            EditorUtility.DisplayDialog("无法保存", "装备 ID 不能重复。", "确定");
            return;
        }

        var absolutePath = Path.GetFullPath(ConfigAssetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
        // JsonUtility 无法稳定处理 int[][]；LitJson 可保持 ShapeMatrix 的二维数组结构。
        File.WriteAllText(absolutePath, JsonMapper.ToJson(_config));
        AssetDatabase.ImportAsset(ConfigAssetPath);
        _dirty = false;
    }

    private string GetUniqueId(string prefix)
    {
        var candidate = prefix;
        var suffix = 1;
        while (_config.items.Any(item => item.Id == candidate)) candidate = $"{prefix}_{suffix++}";
        return candidate;
    }

    private void RestoreShapeMatrices(string json)
    {
        var root = JsonMapper.ToObject<JsonData>(json);
        if (root == null || !root.ContainsKey("items")) return;
        var entries = root["items"];
        for (var index = 0; index < Mathf.Min(entries.Count, _config.items.Count); index++)
        {
            var source = entries[index];
            if (!source.ContainsKey("ShapeMatrix")) continue;
            var matrix = source["ShapeMatrix"];
            if (!matrix.IsArray) continue;

            var rows = new int[matrix.Count][];
            for (var y = 0; y < matrix.Count; y++)
            {
                var row = matrix[y];
                rows[y] = new int[row.Count];
                for (var x = 0; x < row.Count; x++) rows[y][x] = (int)row[x];
            }
            _config.items[index].ShapeMatrix = rows;
        }
    }

    private bool RestoreMissingShapes()
    {
        var restored = false;
        foreach (var item in _config.items)
        {
            if (item.ShapeMatrix != null && item.ShapeMatrix.Length > 0) continue;
            item.ShapeMatrix = GetBuiltinShape(item.Id);
            restored = true;
        }
        return restored;
    }

    private static int[][] GetBuiltinShape(string id)
    {
        switch (id)
        {
            case "rusty_sword":
            case "fire_sword": return new[] { new[] { 1 }, new[] { 1 }, new[] { 1 } };
            case "wooden_shield":
            case "magic_ring": return new[] { new[] { 1, 1 }, new[] { 1, 1 } };
            case "dagger": return new[] { new[] { 1, 0 }, new[] { 0, 1 } };
            case "broadsword": return new[] { new[] { 1, 1, 1, 0 }, new[] { 0, 0, 0, 1 } };
            case "viking_helmet": return new[] { new[] { 1, 1, 1 }, new[] { 0, 1, 0 } };
            case "health_potion": return new[] { new[] { 1 } };
            case "battle_axe": return new[] { new[] { 1, 1 }, new[] { 1, 0 }, new[] { 1, 0 } };
            case "plate_armor": return new[] { new[] { 1, 1, 1 }, new[] { 1, 0, 1 } };
            case "lucky_charm": return new[] { new[] { 0, 1, 0 }, new[] { 1, 0, 1 } };
            case "dragon_slayer": return new[] { new[] { 1, 1, 1, 1 }, new[] { 0, 0, 1, 0 } };
            case "spear": return new[] { new[] { 1 }, new[] { 1 }, new[] { 0 }, new[] { 1 } };
            default: return new[] { new[] { 1 } };
        }
    }

    private static int[,] ToSquareShape(int[][] source)
    {
        var result = new int[ShapeSize, ShapeSize];
        if (source == null) return result;
        for (var y = 0; y < Mathf.Min(source.Length, ShapeSize); y++)
            for (var x = 0; source[y] != null && x < Mathf.Min(source[y].Length, ShapeSize); x++)
                result[x, y] = source[y][x] == 0 ? 0 : 1;
        return result;
    }

    private static int[][] TrimShape(int[,] source)
    {
        var maxX = -1;
        var maxY = -1;
        for (var x = 0; x < ShapeSize; x++)
            for (var y = 0; y < ShapeSize; y++)
                if (source[x, y] != 0) { maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y); }

        if (maxX < 0) return new[] { new[] { 1 } };
        var result = new int[maxY + 1][];
        for (var y = 0; y <= maxY; y++)
        {
            result[y] = new int[maxX + 1];
            for (var x = 0; x <= maxX; x++) result[y][x] = source[x, y];
        }
        return result;
    }
}
