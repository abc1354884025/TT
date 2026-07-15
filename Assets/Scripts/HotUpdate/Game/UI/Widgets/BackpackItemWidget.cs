using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>背包中的装备渲染器。形状底板和装备视觉分层，视觉可以是 Sprite 或带 Animator 的 Prefab。</summary>
public class BackpackItemWidget : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image _background;
    [SerializeField] private RarityBadgeWidget _rarityBadge;

    public PlacedItem PlacedItem { get; private set; }

    private BackpackGridWidget _grid;
    private PrepareViewModel _vm;
    private RectTransform _shapeRoot;
    private RectTransform _visualRoot;
    private GameObject _fallbackIcon;

    public void Init(PlacedItem placedItem, float cellSize, BackpackGridWidget grid = null, PrepareViewModel vm = null)
    {
        PlacedItem = placedItem;
        _grid = grid;
        _vm = vm;

        var shape = placedItem.RotatedShape;
        var rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(shape.Width * cellSize, shape.Height * cellSize);
        rt.pivot = new Vector2(0, 1);

        if (_background)
            _background.color = GetColorByRarity(placedItem.ItemData.Rarity);
        if (_rarityBadge)
            _rarityBadge.SetRarity(placedItem.ItemData.Rarity);

        EnsureRoots();
        RenderShapeBlocks(shape, cellSize);
        LoadVisual(placedItem, shape, cellSize);

        if (!_background)
            _background = GetComponent<Image>();
        if (_background)
            _background.raycastTarget = true;
    }

    private void EnsureRoots()
    {
        if (_shapeRoot == null)
            _shapeRoot = CreateRoot("ShapeRoot");
        if (_visualRoot == null)
            _visualRoot = CreateRoot("VisualRoot");
    }

    private RectTransform CreateRoot(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var root = go.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0, 1);
        root.anchorMax = new Vector2(0, 1);
        root.pivot = new Vector2(0, 1);
        root.anchoredPosition = Vector2.zero;
        return root;
    }

    private void RenderShapeBlocks(ItemShape shape, float cellSize)
    {
        foreach (Transform child in _shapeRoot) Destroy(child.gameObject);

        for (int sx = 0; sx < shape.Width; sx++)
        {
            for (int sy = 0; sy < shape.Height; sy++)
            {
                if (!shape.Cells[sx, sy]) continue;
                var block = new GameObject("Cell", typeof(Image));
                block.transform.SetParent(_shapeRoot, false);
                block.GetComponent<Image>().color = _background ? _background.color : Color.white;
                block.GetComponent<Image>().raycastTarget = false;

                var rt = block.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(sx * cellSize, -sy * cellSize);
                rt.sizeDelta = new Vector2(cellSize, cellSize);
            }
        }
    }

    private void LoadVisual(PlacedItem placedItem, ItemShape shape, float cellSize)
    {
        foreach (Transform child in _visualRoot) Destroy(child.gameObject);
        _fallbackIcon = null;
        _visualRoot.sizeDelta = new Vector2(shape.Width * cellSize, shape.Height * cellSize);

        var visualPath = placedItem.ItemData.BackpackVisualPath;
        if (string.IsNullOrEmpty(visualPath))
        {
            CreateFallbackIcon(placedItem.ItemData.IconPath, shape, cellSize);
            return;
        }

        var expectedInstanceId = placedItem.InstanceId;
        ResourceManager.InstantiateAsync(visualPath, _visualRoot, visual =>
        {
            // 背包刷新可能发生在异步资源回调之前。
            if (this == null || PlacedItem == null || PlacedItem.InstanceId != expectedInstanceId)
            {
                if (visual) Destroy(visual);
                return;
            }

            if (visual == null)
            {
                CreateFallbackIcon(placedItem.ItemData.IconPath, shape, cellSize);
                return;
            }

            var visualRt = visual.GetComponent<RectTransform>();
            if (visualRt != null)
            {
                visualRt.anchorMin = Vector2.zero;
                visualRt.anchorMax = Vector2.one;
                visualRt.offsetMin = Vector2.zero;
                visualRt.offsetMax = Vector2.zero;
                visualRt.localRotation = Quaternion.Euler(0f, 0f, -90f * placedItem.Rotation);
            }

            var animator = visual.GetComponentInChildren<Animator>();
            if (animator != null && !string.IsNullOrEmpty(placedItem.ItemData.BackpackAnimationState))
                animator.Play(placedItem.ItemData.BackpackAnimationState, 0, 0f);
        });
    }

    private void CreateFallbackIcon(string iconPath, ItemShape shape, float cellSize)
    {
        if (string.IsNullOrEmpty(iconPath) || _fallbackIcon != null) return;

        _fallbackIcon = new GameObject("FallbackIcon", typeof(Image));
        _fallbackIcon.transform.SetParent(_visualRoot, false);
        var iconImg = _fallbackIcon.GetComponent<Image>();
        iconImg.sprite = ResourceManager.LoadSprite(iconPath);
        iconImg.raycastTarget = false;
        iconImg.preserveAspect = true;

        var iconRt = _fallbackIcon.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = new Vector2(shape.Width * cellSize / 2, -shape.Height * cellSize / 2);
        iconRt.sizeDelta = new Vector2(cellSize, cellSize);
        iconRt.localRotation = Quaternion.Euler(0f, 0f, -90f * PlacedItem.Rotation);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (PlacedItem == null || DragDropManager.Instance == null) return;
        if (_grid == null) _grid = GetComponentInParent<BackpackGridWidget>();
        DragDropManager.Instance.BeginDragFromGrid(this, _vm, _grid);
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        DragDropManager.Instance?.EndDrag();
    }

    private Color GetColorByRarity(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => new Color(0.5f, 0.5f, 0.5f, 0.8f),
            ItemRarity.Uncommon => new Color(0.2f, 0.7f, 0.2f, 0.8f),
            ItemRarity.Rare => new Color(0.2f, 0.4f, 1.0f, 0.8f),
            ItemRarity.Epic => new Color(0.6f, 0.2f, 1.0f, 0.8f),
            ItemRarity.Legendary => new Color(1.0f, 0.65f, 0.1f, 0.9f),
            _ => Color.gray
        };
    }
}
