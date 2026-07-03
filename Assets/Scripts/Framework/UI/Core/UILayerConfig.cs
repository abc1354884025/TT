using UnityEngine;

/// <summary>
/// UI 层级配置 ScriptableObject。
/// 右键 Create → UI → Layer Config 创建。
/// </summary>
[CreateAssetMenu(fileName = "UILayerConfig", menuName = "UI/Layer Config", order = 1)]
public class UILayerConfig : ScriptableObject
{
    [System.Serializable]
    public class LayerEntry
    {
        public UILayer Layer;
        public int SortingOrder;
        public bool BlockRaycasts = true;
    }

    [SerializeField] private LayerEntry[] _layers;

    public LayerEntry[] Layers => _layers;

    public int GetSortingOrder(UILayer layer)
    {
        foreach (var e in _layers)
            if (e.Layer == layer) return e.SortingOrder;
        return (int)layer;
    }

    public void ResetToDefault()
    {
        _layers = new LayerEntry[]
        {
            new LayerEntry { Layer = UILayer.Background, SortingOrder = 0,   BlockRaycasts = true },
            new LayerEntry { Layer = UILayer.Normal,     SortingOrder = 100, BlockRaycasts = true },
            new LayerEntry { Layer = UILayer.Popup,      SortingOrder = 200, BlockRaycasts = true },
            new LayerEntry { Layer = UILayer.Top,        SortingOrder = 300, BlockRaycasts = true },
            new LayerEntry { Layer = UILayer.System,     SortingOrder = 400, BlockRaycasts = true },
        };
    }
}
