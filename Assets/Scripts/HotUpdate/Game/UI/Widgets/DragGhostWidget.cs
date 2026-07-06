using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 拖拽时显示的半透明 Ghost，跟随鼠标，绿色=可放置，红色=不可。
/// </summary>
public class DragGhostWidget : MonoBehaviour
{
    [SerializeField] private Image _mainImage;
    [SerializeField] private float _alpha = 0.6f;

    private float _cellSize;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void Show(ItemData data, float cellSize)
    {
        _cellSize = cellSize;
        gameObject.SetActive(true);
        RefreshShape(data, 0);
    }

    public void RefreshShape(ItemData data, int rotation)
    {
        if (data == null) return;
        var shape = data.GetShape().Rotate(rotation);

        _rectTransform.sizeDelta = new Vector2(shape.Width * _cellSize, shape.Height * _cellSize);

        // 清除旧子对象，重新渲染形状
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        for (int sx = 0; sx < shape.Width; sx++)
        {
            for (int sy = 0; sy < shape.Height; sy++)
            {
                if (!shape.Cells[sx, sy]) continue;
                var block = new GameObject("Cell", typeof(Image));
                block.transform.SetParent(transform, false);
                var img = block.GetComponent<Image>();
                img.color = new Color(1, 1, 1, _alpha);

                var rt = block.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(sx * _cellSize, -sy * _cellSize);
                rt.sizeDelta = new Vector2(_cellSize, _cellSize);
            }
        }
    }

    public void SetValid(bool valid)
    {
        if (_mainImage == null) return;
        _mainImage.color = valid
            ? new Color(0, 1, 0, _alpha * 0.5f)  // 绿
            : new Color(1, 0, 0, _alpha * 0.5f);  // 红
    }

    public void SnapToGrid(Vector2Int cell, float cellSize)
    {
        // Position adjusted by parent; set in DragDropManager via transform.localPosition
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
