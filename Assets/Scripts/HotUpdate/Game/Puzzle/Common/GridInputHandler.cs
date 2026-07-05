using System;
using UnityEngine;
using UnityEngine.EventSystems;

    /// <summary>
    /// 统一网格输入处理器。将触摸/鼠标输入转换为 PuzzleMove 对象。
    /// 挂载到网格区域的 GameObject 上。所有益智游戏共用此组件。
    /// </summary>
    public class GridInputHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler, IDragHandler
    {
        [Header("设置")]
        [SerializeField] private float _dragThreshold = 10f;     // 移动多少像素视为拖拽
        [SerializeField] private bool _enableDrag = true;         // 是否启用拖拽（数独可设为 false）
        [SerializeField] private RectTransform _gridArea;         // 网格区域 RectTransform

        private Vector2Int _lastCell = new Vector2Int(-1, -1);
        private Vector2Int _dragStartCell = new Vector2Int(-1, -1);
        private bool _isDragging;
        private Vector2 _pointerDownPos;
        private int _gridWidth;
        private int _gridHeight;

        /// <summary>当产生有效操作时触发</summary>
        public event Action<PuzzleMove> OnMove;

        /// <summary>当指针悬停在某个格子上时触发（拖拽过程中）</summary>
        public event Action<Vector2Int> OnCellHover;

        /// <summary>设置网格尺寸（由游戏面板在初始化时调用）</summary>
        public void SetGridSize(int width, int height)
        {
            _gridWidth = width;
            _gridHeight = height;
        }

        /// <summary>启用/禁用拖拽模式（数独设为 false）</summary>
        public void SetDragEnabled(bool enabled)
        {
            _enableDrag = enabled;
        }

        /// <summary>将屏幕坐标转换为网格坐标。返回 (-1,-1) 表示不在网格范围内。</summary>
        public Vector2Int ScreenToGrid(Vector2 screenPos)
        {
            if (_gridArea == null || _gridWidth <= 0 || _gridHeight <= 0)
                return new Vector2Int(-1, -1);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _gridArea, screenPos, null, out Vector2 localPos);

            float areaW = _gridArea.rect.width;
            float areaH = _gridArea.rect.height;
            float cellW = areaW / _gridWidth;
            float cellH = areaH / _gridHeight;

            // 左上角为原点
            int x = Mathf.FloorToInt(localPos.x / cellW);
            int y = Mathf.FloorToInt((areaH - localPos.y) / cellH);  // y 轴翻转

            return (x >= 0 && x < _gridWidth && y >= 0 && y < _gridHeight)
                ? new Vector2Int(x, y)
                : new Vector2Int(-1, -1);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isDragging = false;
            _pointerDownPos = eventData.position;
            _dragStartCell = ScreenToGrid(eventData.position);
            _lastCell = _dragStartCell;

            if (_dragStartCell.x >= 0)
            {
                if (_enableDrag)
                {
                    // 拖拽模式：拖拽开始时通知
                    var move = new PuzzleMove
                    {
                        ActionType = InputActionType.DragStart,
                        Position = _dragStartCell
                    };
                    OnMove?.Invoke(move);
                }
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_enableDrag) return;

            Vector2 delta = eventData.position - _pointerDownPos;
            if (delta.magnitude >= _dragThreshold)
                _isDragging = true;

            if (_isDragging)
            {
                Vector2Int currentCell = ScreenToGrid(eventData.position);
                if (currentCell.x >= 0 && currentCell != _lastCell)
                {
                    _lastCell = currentCell;
                    OnCellHover?.Invoke(currentCell);

                    var move = new PuzzleMove
                    {
                        ActionType = InputActionType.DragEnter,
                        Position = currentCell
                    };
                    OnMove?.Invoke(move);
                }
            }
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            // 仅用于非拖拽模式的悬停事件
            if (_isDragging || _enableDrag) return;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isDragging)
            {
                // 单击模式
                Vector2Int cell = ScreenToGrid(eventData.position);
                if (cell.x >= 0)
                {
                    var move = new PuzzleMove
                    {
                        ActionType = InputActionType.Tap,
                        Position = cell
                    };
                    OnMove?.Invoke(move);
                }
            }
            else
            {
                // 拖拽结束
                Vector2Int endCell = ScreenToGrid(eventData.position);
                if (_dragStartCell.x >= 0)
                {
                    var move = new PuzzleMove
                    {
                        ActionType = InputActionType.DragEnd,
                        Position = _dragStartCell,
                        SecondaryPos = endCell.x >= 0 ? endCell : _lastCell
                    };
                    OnMove?.Invoke(move);
                }
            }
            _isDragging = false;
            _lastCell = new Vector2Int(-1, -1);
            _dragStartCell = new Vector2Int(-1, -1);
        }
    }
