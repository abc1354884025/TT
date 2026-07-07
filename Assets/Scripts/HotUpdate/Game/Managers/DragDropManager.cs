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

    /// <summary>拖拽结束后触发（由 PreparePanel 订阅来刷新 UI）</summary>
    public event System.Action OnDragFinished;

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

        // 先把 Widget 移到 Canvas 下，避免 RefreshItems 销毁它
        if (_canvas != null) widget.transform.SetParent(_canvas.transform, true);

        // 从网格中移除（会触发 RefreshItems，但 Widget 已不在 grid 子节点中，不会被销毁）
        if (widget.PlacedItem != null && _vm != null)
            _vm.BagGrid.RemoveItem(widget.PlacedItem);

        CreateGhost(DraggedItemData);
    }

    // ====== 帧更新 ======

    private void Update()
    {
        if (!_isDragging || _targetGrid == null) return;

        // Ghost 跟随鼠标
        if (Ghost != null && _canvas != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvas.transform as RectTransform, Input.mousePosition, _canvas.worldCamera, out var localPos);
            Ghost.transform.localPosition = localPos;
        }

        // 预览放置
        var cell = _targetGrid.ScreenToGrid(Input.mousePosition);
        if (cell.HasValue && DraggedItemData != null)
        {
            var shape = DraggedItemData.GetShape().Rotate(CurrentRotation);
            bool valid = _vm.BagGrid.CanPlace(shape, cell.Value.x, cell.Value.y);
            Ghost?.SetValid(valid);
            _targetGrid.ShowPlacementPreview(shape, cell.Value.x, cell.Value.y, valid);
        }
        else
        {
            _targetGrid.ClearPreview();
        }
    }

    // ====== 结束拖拽 ======

    public void EndDrag()
    {
        if (!_isDragging) return;

        _targetGrid?.ClearPreview();

        if (_vm != null && DraggedItemData != null)
        {
            var cell = _targetGrid?.ScreenToGrid(Input.mousePosition);
            Debug.Log($"[EndDrag] cell={cell}, curRot={CurrentRotation}");
            if (cell.HasValue)
            {
                var result = _vm.BagGrid.PlaceItem(DraggedItemData, cell.Value.x, cell.Value.y, CurrentRotation);
                if (result != null)
                    Debug.Log($"[EndDrag] 放置成功 @ ({cell.Value.x},{cell.Value.y})");
                else
                {
                    Debug.Log($"[EndDrag] 放置失败，退回物品栏");
                    _vm.Inventory.Add(DraggedItemData);
                }
            }
            else
            {
                Debug.Log("[EndDrag] 拖到网格外，退回物品栏");
                _vm.Inventory.Add(DraggedItemData);
            }
        }
        else
        {
            Debug.LogWarning($"[DragDropManager] EndDrag 跳过: _vm={_vm != null}, item={DraggedItemData != null}");
        }

        if (Ghost != null) Destroy(Ghost.gameObject);
        Ghost = null;

        // 从网格拖出的 Widget 不再需要（放置成功会由 RefreshItems 重建，失败则回到物品栏）
        if (_draggedGridItem != null) Destroy(_draggedGridItem.gameObject);

        _draggedGridItem = null;
        DraggedItemData = null;
        _isDragging = false;

        OnDragFinished?.Invoke();
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
        if (_canvas == null || data == null) return;

        GameObject ghostGo;
        if (_ghostPrefab != null)
            ghostGo = Instantiate(_ghostPrefab.gameObject, _canvas.transform);
        else
        {
            // 没有预制体时自动创建一个哑元 Ghost
            ghostGo = new GameObject("DragGhost", typeof(RectTransform), typeof(DragGhostWidget));
            ghostGo.transform.SetParent(_canvas.transform, false);
        }

        Ghost = ghostGo.GetComponent<DragGhostWidget>();
        float cellSize = _targetGrid != null ? _targetGrid.CellSize : 64f;
        Ghost.Show(data, cellSize);
    }
}
