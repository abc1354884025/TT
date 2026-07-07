using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 拖拽 Ghost——半透明跟随鼠标。绿色=可放置，红色=不可。
/// 预制的 cell block 由 SetValid 统一着色。
/// </summary>
public class DragGhostWidget : MonoBehaviour
{
    [SerializeField] private Image _mainImage;
    [SerializeField] private float _alpha = 0.6f;

    private float _cellSize;
    private RectTransform _rectTransform;
    private List<Image> _cellBlocks = new List<Image>();
    private bool _isValid = true;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        // 没有 Inspector 赋值时自动创建背景
        if (!_mainImage)
            _mainImage = gameObject.AddComponent<Image>();
        _mainImage.raycastTarget = false;
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

        // 清除旧子块
        foreach (var block in _cellBlocks)
            Destroy(block.gameObject);
        _cellBlocks.Clear();

        // 重建形状块
        for (int sx = 0; sx < shape.Width; sx++)
        {
            for (int sy = 0; sy < shape.Height; sy++)
            {
                if (!shape.Cells[sx, sy]) continue;
                var block = new GameObject("Cell", typeof(Image));
                block.transform.SetParent(transform, false);
                block.hideFlags = HideFlags.DontSave;
                var img = block.GetComponent<Image>();
                img.raycastTarget = false;
                _cellBlocks.Add(img);

                var rt = block.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(sx * _cellSize, -sy * _cellSize);
                rt.sizeDelta = new Vector2(_cellSize, _cellSize);
            }
        }

        ApplyTint();
    }

    public void SetValid(bool valid)
    {
        _isValid = valid;
        ApplyTint();
    }

    private void ApplyTint()
    {
        Color c = _isValid
            ? new Color(0, 1, 0, _alpha * 0.5f)
            : new Color(1, 0, 0, _alpha * 0.5f);

        if (_mainImage)
            _mainImage.color = c;

        foreach (var block in _cellBlocks)
        {
            if (block) block.color = c;
        }
    }

    public void SnapToGrid(Vector2Int cell, float cellSize) { }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
