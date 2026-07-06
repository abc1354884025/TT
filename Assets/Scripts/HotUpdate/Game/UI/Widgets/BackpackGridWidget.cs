using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包网格渲染器——核心 UI Widget。
/// 负责绘制网格线、管理物品实例、坐标转换。
/// </summary>
public class BackpackGridWidget : MonoBehaviour
{
    [Header("网格设置")]
    [SerializeField] private float _cellSize = 64f;
    [SerializeField] private Color _cellColorA = new Color(0.2f, 0.2f, 0.25f);
    [SerializeField] private Color _cellColorB = new Color(0.25f, 0.25f, 0.30f);
    [SerializeField] private Color _cellBorderColor = new Color(0.15f, 0.15f, 0.18f);

    [Header("高亮")]
    [SerializeField] private GameObject _highlightPrefab; // 绿色/红色高亮方块（可选）

    /// <summary>物品 Prefab（BackpackItemWidget 挂载的 Prefab）</summary>
    [SerializeField] private GameObject _itemWidgetPrefab;

    private BagGrid _grid;
    private RectTransform _rectTransform;
    private List<BackpackItemWidget> _itemWidgets = new List<BackpackItemWidget>();
    private List<GameObject> _cellHighlights = new List<GameObject>();
    private ObjectPool<GameObject> _highlightPool;

    public float CellSize => _cellSize;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>绑定网格数据并绘制</summary>
    public void Bind(BagGrid grid)
    {
        _grid = grid;
        _grid.OnChanged += RefreshItems;
        DrawGridBackground();
        RefreshItems();
    }

    /// <summary>绘制棋盘格背景</summary>
    private void DrawGridBackground()
    {
        var bgGo = new GameObject("GridBg", typeof(Image));
        bgGo.transform.SetParent(transform, false);
        var bgImg = bgGo.GetComponent<Image>();
        bgImg.color = _cellColorA;
        var bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        // 绘制交替色格子和边框
        for (int x = 0; x < _grid.Width; x++)
        {
            for (int y = 0; y < _grid.Height; y++)
            {
                var cell = new GameObject($"Cell_{x}_{y}", typeof(Image));
                cell.transform.SetParent(transform, false);
                var img = cell.GetComponent<Image>();
                img.color = (x + y) % 2 == 0 ? _cellColorA : _cellColorB;
                img.raycastTarget = false;

                var rt = cell.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(x * _cellSize, -y * _cellSize);
                rt.sizeDelta = new Vector2(_cellSize, _cellSize);
            }
        }
    }

    /// <summary>刷新所有物品 Widget</summary>
    private void RefreshItems()
    {
        // 清除旧 Widget
        foreach (var w in _itemWidgets)
            Destroy(w.gameObject);
        _itemWidgets.Clear();

        if (_grid == null) return;

        // 创建新 Widget
        foreach (var placedItem in _grid.Items)
        {
            if (_itemWidgetPrefab == null)
            {
                Debug.LogError("[BackpackGridWidget] 未设置 ItemWidgetPrefab");
                return;
            }

            var go = Instantiate(_itemWidgetPrefab, transform);
            var widget = go.GetComponent<BackpackItemWidget>();
            if (widget == null)
                widget = go.AddComponent<BackpackItemWidget>();

            widget.Init(placedItem, _cellSize);

            // 定位到格子位置
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(placedItem.GridX * _cellSize, -placedItem.GridY * _cellSize);

            _itemWidgets.Add(widget);
        }
    }

    /// <summary>屏幕坐标转格子坐标</summary>
    public Vector2Int? ScreenToGrid(Vector2 screenPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform, screenPoint, null, out var localPoint);

        // 以左上为原点
        float relX = localPoint.x - _rectTransform.rect.xMin;
        float relY = _rectTransform.rect.yMax - localPoint.y;

        int gx = Mathf.FloorToInt(relX / _cellSize);
        int gy = Mathf.FloorToInt(relY / _cellSize);

        if (_grid == null) return new Vector2Int(gx, gy);

        if (gx >= 0 && gx < _grid.Width && gy >= 0 && gy < _grid.Height)
            return new Vector2Int(gx, gy);
        return null;
    }

    /// <summary>格子坐标转世界坐标（物品放置锚点）</summary>
    public Vector2 GridToAnchoredPosition(int gx, int gy)
    {
        return new Vector2(gx * _cellSize, -gy * _cellSize);
    }

    private void OnDestroy()
    {
        if (_grid != null)
            _grid.OnChanged -= RefreshItems;
    }
}
