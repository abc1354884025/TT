using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包网格渲染器——核心 UI Widget。
/// 负责绘制网格背景、管理物品实例、坐标转换、拖拽预览高亮。
/// </summary>
public class BackpackGridWidget : MonoBehaviour
{
    [Header("网格设置")]
    [SerializeField] private float _cellSize = 64f;
    [SerializeField] private Color _cellColorA = new Color(0.2f, 0.2f, 0.25f);
    [SerializeField] private Color _cellColorB = new Color(0.25f, 0.25f, 0.30f);

    [Header("预览")]
    [SerializeField] private Color _validColor = new Color(0, 1, 0, 0.3f);
    [SerializeField] private Color _invalidColor = new Color(1, 0, 0, 0.3f);

    /// <summary>物品 Prefab</summary>
    [SerializeField] private GameObject _itemWidgetPrefab;

    private BagGrid _grid;
    private RectTransform _rectTransform;
    private List<BackpackItemWidget> _itemWidgets = new List<BackpackItemWidget>();
    private List<GameObject> _previewHighlights = new List<GameObject>();
    private PrepareViewModel _vm;

    public float CellSize => _cellSize;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void Bind(BagGrid grid, PrepareViewModel vm = null)
    {
        _grid = grid;
        _vm = vm;
        _grid.OnChanged += RefreshItems;
        DrawGridBackground();
        RefreshItems();
    }

    // ====== 绘制背景 ======

    private void DrawGridBackground()
    {
        var bgGo = new GameObject("GridBg", typeof(Image));
        bgGo.transform.SetParent(transform, false);
        bgGo.GetComponent<Image>().color = _cellColorA;
        bgGo.GetComponent<Image>().raycastTarget = false;
        var bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

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
                rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(x * _cellSize, -y * _cellSize);
                rt.sizeDelta = new Vector2(_cellSize, _cellSize);
            }
        }
    }

    // ====== 物品 ======

    private void RefreshItems()
    {
        foreach (var w in _itemWidgets) Destroy(w.gameObject);
        _itemWidgets.Clear();
        if (_grid == null) return;

        foreach (var placedItem in _grid.Items)
        {
            if (_itemWidgetPrefab == null) return;
            var go = Instantiate(_itemWidgetPrefab, transform);
            var widget = go.GetComponent<BackpackItemWidget>();
            if (!widget) widget = go.AddComponent<BackpackItemWidget>();
            widget.Init(placedItem, _cellSize, this, _vm);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(placedItem.GridX * _cellSize, -placedItem.GridY * _cellSize);
            _itemWidgets.Add(widget);
        }
    }

    // ====== 拖拽预览高亮 ======

    /// <summary>显示放置预览（绿色/红色格子高亮）</summary>
    public void ShowPlacementPreview(ItemShape shape, int gx, int gy, bool valid)
    {
        ClearPreview();
        if (shape == null || _grid == null) return;

        Color color = valid ? _validColor : _invalidColor;

        for (int sx = 0; sx < shape.Width; sx++)
        {
            for (int sy = 0; sy < shape.Height; sy++)
            {
                if (!shape.Cells[sx, sy]) continue;
                int cx = gx + sx, cy = gy + sy;
                if (cx < 0 || cx >= _grid.Width || cy < 0 || cy >= _grid.Height) continue;

                var go = new GameObject("Preview", typeof(Image));
                go.transform.SetParent(transform, false);
                go.GetComponent<Image>().color = color;
                go.GetComponent<Image>().raycastTarget = false;

                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(cx * _cellSize, -cy * _cellSize);
                rt.sizeDelta = new Vector2(_cellSize, _cellSize);

                _previewHighlights.Add(go);
            }
        }
    }

    /// <summary>清除预览高亮</summary>
    public void ClearPreview()
    {
        foreach (var go in _previewHighlights) Destroy(go);
        _previewHighlights.Clear();
    }

    // ====== 坐标 ======

    public Vector2Int? ScreenToGrid(Vector2 screenPoint)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform, screenPoint, null, out var localPoint);

        float relX = localPoint.x - _rectTransform.rect.xMin;
        float relY = _rectTransform.rect.yMax - localPoint.y;

        int gx = Mathf.FloorToInt(relX / _cellSize);
        int gy = Mathf.FloorToInt(relY / _cellSize);
        if (_grid == null) return new Vector2Int(gx, gy);
        if (gx >= 0 && gx < _grid.Width && gy >= 0 && gy < _grid.Height)
            return new Vector2Int(gx, gy);
        return null;
    }

    public Vector2 GridToAnchoredPosition(int gx, int gy)
        => new Vector2(gx * _cellSize, -gy * _cellSize);

    private void OnDestroy()
    {
        if (_grid != null) _grid.OnChanged -= RefreshItems;
    }
}
