using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 拖放管理器——支持从物品栏拖拽到网格，以及网格内拖拽。
/// 实时预览放置位置，绿=可放，红=不可。
/// 挂载在场景 Canvas 下的常驻 GameObject 上。
/// </summary>
public class DragDropManager : MonoBehaviour
{
    public static DragDropManager Instance { get; private set; }

    [SerializeField] private DragGhostWidget _ghostPrefab;

    public bool IsDragging => _isDragging;
    public ItemData DraggedItemData { get; private set; }
    public int CurrentRotation { get; private set; }
    public DragGhostWidget Ghost { get; private set; }

    private bool _isDragging;
    private BackpackItemWidget _draggedGridItem; // 从网格拖拽的已有物品
    private BackpackGridWidget _targetGrid;
    private PrepareViewModel _vm;
    private Canvas _canvas;

    private void Awake()
    {
        Instance = this;
        _canvas = GetComponentInParent<Canvas>();
    }

    // ====== 从物品栏拖拽（ItemData，未放置） ======

    /// <summary>从物品栏开始拖拽一个新物品</summary>
    public void BeginDragFromInventory(ItemData itemData, PrepareViewModel vm, BackpackGridWidget grid)
    {
        _isDragging = true;
        DraggedItemData = itemData;
        _draggedGridItem = null;
        _targetGrid = grid;
        _vm = vm;
        CurrentRotation = 0;

        CreateGhost(itemData);
    }

    // ====== 从网格内拖拽（已放置物品） ======

    public void BeginDragFromGrid(BackpackItemWidget widget, PrepareViewModel vm, BackpackGridWidget grid)
    {
        _isDragging = true;
        _draggedGridItem = widget;
        _targetGrid = grid;
        _vm = vm;
        DraggedItemData = widget.PlacedItem?.ItemData;
        CurrentRotation = widget.PlacedItem?.Rotation ?? 0;

        // 从网格中临时移除
        if (widget.PlacedItem != null)
            _vm.BagGrid.RemoveItem(widget.PlacedItem);

        CreateGhost(DraggedItemData);
    }

    // ====== 帧更新 ======

    private void Update()
    {
        if (!_isDragging || Ghost == null || _targetGrid == null) return;

        // Ghost 跟随鼠标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform, Input.mousePosition, _canvas.worldCamera, out var localPos);
        Ghost.transform.localPosition = localPos;

        // 预览放置
        var cell = _targetGrid.ScreenToGrid(Input.mousePosition);
        if (cell.HasValue && DraggedItemData != null)
        {
            var shape = DraggedItemData.GetShape().Rotate(CurrentRotation);
            bool valid = _vm.BagGrid.CanPlace(shape, cell.Value.x, cell.Value.y);
            Ghost.SetValid(valid);
        }
    }

    // ====== 结束拖拽 ======

    public void EndDrag()
    {
        if (!_isDragging) return;

        var cell = _targetGrid?.ScreenToGrid(Input.mousePosition);
        bool placed = false;

        if (cell.HasValue && DraggedItemData != null)
        {
            var placedItem = _vm.BagGrid.PlaceItem(DraggedItemData, cell.Value.x, cell.Value.y, CurrentRotation);
            placed = placedItem != null;
        }

        if (!placed && DraggedItemData != null)
        {
            // 放回物品栏
            _vm.Inventory.Add(DraggedItemData);
        }

        // 清理
        if (Ghost != null) Destroy(Ghost.gameObject);
        Ghost = null;
        _draggedGridItem = null;
        DraggedItemData = null;
        _isDragging = false;
    }

    // ====== 旋转 ======

    /// <summary>旋转当前拖拽的物品（右键/双指）</summary>
    public void RotateDraggedItem()
    {
        if (!_isDragging || DraggedItemData == null) return;
        CurrentRotation = (CurrentRotation + 1) % 4;
        if (Ghost != null)
            Ghost.RefreshShape(DraggedItemData, CurrentRotation);
    }

    // ====== 辅助 ======

    private void CreateGhost(ItemData data)
    {
        if (_ghostPrefab == null || _canvas == null) return;
        var ghostGo = Instantiate(_ghostPrefab.gameObject, _canvas.transform);
        Ghost = ghostGo.GetComponent<DragGhostWidget>();
        Ghost?.Show(data, _targetGrid != null ? _targetGrid.CellSize : 64f);
    }
}
