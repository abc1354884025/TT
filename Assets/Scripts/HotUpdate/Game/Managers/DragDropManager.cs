using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 拖放管理器——管理物品拖拽状态、Ghost 渲染、放置校验。
/// 挂载在场景常驻 GameObject 上。
/// </summary>
public class DragDropManager : MonoBehaviour
{
    public static DragDropManager Instance { get; private set; }

    [SerializeField] private DragGhostWidget _ghostPrefab;

    /// <summary>当前拖拽状态</summary>
    public bool IsDragging => _draggedItem != null;
    public BackpackItemWidget DraggedWidget => _draggedItem;
    public DragGhostWidget Ghost { get; private set; }

    private BackpackItemWidget _draggedItem;
    private BackpackGridWidget _targetGrid;
    private PrepareViewModel _vm;
    private Canvas _canvas;

    private void Awake()
    {
        Instance = this;
        _canvas = GetComponentInParent<Canvas>();
    }

    /// <summary>开始拖拽（由 BackpackItemWidget 调用）</summary>
    public void BeginDrag(BackpackItemWidget widget, PointerEventData eventData, PrepareViewModel vm, BackpackGridWidget grid)
    {
        _draggedItem = widget;
        _targetGrid = grid;
        _vm = vm;

        // 从网格中移除
        if (widget.PlacedItem != null)
            _vm.BagGrid.RemoveItem(widget.PlacedItem);

        // 创建 Ghost
        if (_ghostPrefab != null)
        {
            var ghostGo = Instantiate(_ghostPrefab.gameObject, _canvas.transform);
            Ghost = ghostGo.GetComponent<DragGhostWidget>();
            if (Ghost != null)
                Ghost.Show(widget.PlacedItem?.ItemData, _targetGrid.CellSize);
        }
    }

    /// <summary>拖拽中（每帧 Update）</summary>
    private void Update()
    {
        if (!IsDragging || Ghost == null || _targetGrid == null) return;

        // Ghost 跟随鼠标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.transform as RectTransform, Input.mousePosition, _canvas.worldCamera, out var localPos);
        Ghost.transform.localPosition = localPos;

        // 检测放置位置
        var cell = _targetGrid.ScreenToGrid(Input.mousePosition);
        if (cell.HasValue && _draggedItem.PlacedItem != null)
        {
            bool valid = _vm.BagGrid.CanPlace(_draggedItem.PlacedItem.RotatedShape, cell.Value.x, cell.Value.y);
            Ghost.SetValid(valid);
            Ghost.SnapToGrid(cell.Value, _targetGrid.CellSize);
        }
    }

    /// <summary>结束拖拽</summary>
    public void EndDrag(PointerEventData eventData)
    {
        if (!IsDragging) return;

        var cell = _targetGrid?.ScreenToGrid(Input.mousePosition);
        bool placed = false;

        if (cell.HasValue && _draggedItem.PlacedItem != null)
        {
            var item = _vm.BagGrid.PlaceItem(
                _draggedItem.PlacedItem.ItemData,
                cell.Value.x,
                cell.Value.y,
                _draggedItem.PlacedItem.Rotation);

            placed = item != null;
        }

        if (!placed && _draggedItem.PlacedItem != null)
        {
            // 放回物品栏
            _vm.Inventory.Add(_draggedItem.PlacedItem.ItemData);
        }

        // 清理
        if (Ghost != null)
            Destroy(Ghost.gameObject);

        Ghost = null;
        _draggedItem = null;
    }

    /// <summary>旋转当前拖拽的物品</summary>
    public void RotateDraggedItem()
    {
        if (_draggedItem?.PlacedItem == null) return;
        _draggedItem.PlacedItem.SetRotation(_draggedItem.PlacedItem.Rotation + 1);
        if (Ghost != null)
            Ghost.RefreshShape(_draggedItem.PlacedItem.ItemData, _draggedItem.PlacedItem.Rotation);
    }
}
