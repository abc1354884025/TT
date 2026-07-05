using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine.Events;

/**
从上到下：
    Content:
        pos: y始终正值，0->1600
        anchor: y,y,y: 1,1,1
    Item:
        pos: y始终负值，-200,-400
        anchor: y,y,y: 1,1,1

从下到上：
    Content:
        pos: y始终负值，0->-1600
        anchor: y,y,y: 0,0,0
    Item:
        pos: y始终正值，200,400
        anchor: y,y,y: 0,0,0

从左至右：
    Content：
        pos: x始终负值，0->-1600
        anchor: x,x,x: 0,0,0
    Item:
        pos: x始终正值，200,400
        anchor: x,x,x: 0,0,0

从右至左：
    Content：
        pos: x始终正值，0->1600
        anchor: x,x,x: 1,1,1
    Item:
        pos: x始终负值，-200,-400
        anchor: x,x,x: 1,1,1

总结：
对应滑动方向轴位anchor的值始终是相同的，而pos的值则是相反的，坐标的位置是正，item pos就会是负
*/

namespace KingSoft
{
    namespace UI
    {
        [RequireComponent(typeof(ScrollRect))]
        public class LoopScrollView : MonoBehaviour, IEndDragHandler, IBeginDragHandler
        {
            // Event
            public class OnCellInitEvent : UnityEvent<GameObject> { }
            public OnCellInitEvent OnCellInit = new OnCellInitEvent();

            public class OnCellUpdateEvent : UnityEvent<int /** index */, GameObject> { }
            public OnCellUpdateEvent OnCellUpdate = new OnCellUpdateEvent();

            // Static
            readonly int CC_INVALID_INDEX = -1;
            readonly int ONE_COLUMN = 1;
            readonly Vector2 VECTOR_2_CENTER = new Vector2(0.5f, 0.5f);

            // Custom Variable
            [Header("Required")]
            public GameObject cellPrefab = null;
            public int cellNumOfColumn = 1;

            public enum EVerticalFillOrder { TOP_DOWN, BOTTOM_UP };
            public EVerticalFillOrder vordering = EVerticalFillOrder.TOP_DOWN;

            [Header("Optional")]
            [Tooltip("The header of the all cell content.")]
            public RectTransform headerNode = null;

            public RectTransform startHintNode = null;

            public RectTransform endHintNode = null;

            public RectTransform emptyTipsNode = null;

            [Tooltip("Each row's spacing.")]
            public float spacing;

            [Tooltip("The padding of the all cell content, include header.")]
            public RectOffset padding = new RectOffset();

            public bool showItemWhenCenterNotInRect = true;

            [Tooltip("If the content is not fill the viewport, center the content in the viewport. Disable ScrollRect and offset the content to center it If not fill the viewport.")]
            public bool centerContentIfNotFillViewport = false;

            [Header("Row Layout")]
            public bool customRowLayout = false;

            [Tooltip("Each column's spacing.")]
            public float columnSpacing;

            [Tooltip("The alignment of the all cell content in the column.")]
            public TextAnchor columnAlignment = TextAnchor.MiddleCenter;

            // Inner Variable
            private enum EDirection { HORIZONTAL, VERTICAL };
            private EDirection direction = EDirection.VERTICAL;

            public delegate Vector2 CellSizeForIndexDelegate(int index);
            private CellSizeForIndexDelegate cellSizeForIndexDelegate = null;

            public delegate void ContentMoveDelegate(int startIndex, int endIndex);
            private ContentMoveDelegate contentMoveDelegate = null;

            private List<float> cellPositions = new List<float>();
            private Dictionary<int, RectTransform> cellsUsed = new Dictionary<int, RectTransform>();
            private Queue<RectTransform> cellsFreed = new Queue<RectTransform>();
            protected ScrollRect scrollRect = null;
            protected RectTransform contentTransform = null;
            protected RectTransform viewportTransform = null;
            private Rect lastViewportRectSize = Rect.zero;
            private Rect lastHeaderRectSize = Rect.zero;
            protected int cellsCount = 0;
            private int cellRows = 0;
            protected bool initialized = false;
            private bool dragFlag = false;
            private bool centerContentActive = false;

            private Vector2 defaultCellSize = Vector2.zero;

            public virtual void OnBeginDrag(PointerEventData eventData)
            {
                if (initialized == false || cellsCount == 0) return;
                dragFlag = true;
            }

            public virtual void OnEndDrag(PointerEventData eventData)
            {
                if (initialized == false || cellsCount == 0) return;
                dragFlag = false;
            }

            public virtual void Initialize(GameObject prefab = null, int count = -1)
            {
                if (initialized) return;
                // 这段逻辑正常应该Start执行一次就行，为啥没执行到呢。。
                scrollRect = gameObject.GetComponent<ScrollRect>();
                if (scrollRect.vertical == true && scrollRect.horizontal == true)
                {
                    Debug.LogError("[LoopScrollView] ScrollRect vertical and horizontal value cannot equal! widget name:" + gameObject.name);
                    return;
                }
                if (scrollRect.content == null || scrollRect.viewport == null)
                {
                    Debug.LogError("[LoopScrollView] ScrollRect content or viewport is null! widget name:" + gameObject.name);
                    return;
                }

                prefab = prefab ?? cellPrefab;
                if (prefab == null)
                {
                    Debug.LogError("[LoopScrollView] cellPrefab is null! widget name:" + gameObject.name);
                    return;
                }

                initialized = true;

                if (scrollRect.vertical && !scrollRect.horizontal) direction = EDirection.VERTICAL;
                else if (scrollRect.horizontal && !scrollRect.vertical) direction = EDirection.HORIZONTAL;

                scrollRect.onValueChanged.AddListener(OnValueChanged);
                contentTransform = scrollRect.content;
                viewportTransform = scrollRect.viewport;
                cellNumOfColumn = cellNumOfColumn >= 1 ? cellNumOfColumn : 1;

                SetPrefab(prefab);
                InitContentTransform();
                if (count > 0) ReloadData(count);
            }

            public void SetPrefab(GameObject newPrefab)
            {
                if (newPrefab == null) return;
                if (newPrefab != cellPrefab || newPrefab.transform.parent == null) cellPrefab = Instantiate(newPrefab, contentTransform);
                cellPrefab.SetActive(false);
                defaultCellSize = cellPrefab.GetComponent<RectTransform>().sizeDelta;
            }

            public virtual void SetCellSizeForIndexDelegate(CellSizeForIndexDelegate del)
            {
                cellSizeForIndexDelegate = del;
            }

            public void SetContentMoveDelegate(ContentMoveDelegate del)
            {
                contentMoveDelegate = del;
            }

            public Vector2 GetDefaultCellSize()
            {
                return defaultCellSize;
            }

            public void ReloadData(int count, bool keepOffset = false)
            {
                if (!initialized)
                {
                    Debug.LogError("[LoopScrollView] LoopScrollView not initialized! widget name:" + gameObject.name);
                    return;
                }
                cellsCount = count;
                cellRows = (int)Math.Ceiling((float)cellsCount / cellNumOfColumn);
                UpdateCellPositions(keepOffset);
                UpdateAllCell();
            }

            public virtual void MoveToCellIndex(int index, float offset = 0)
            {
                if (!initialized)
                {
                    Debug.LogError("[LoopScrollView] LoopScrollView not initialized! widget name:" + gameObject.name);
                    return;
                }
                if (centerContentActive || cellsCount == 0) return;
                index = Math.Min(Math.Max(0, index / cellNumOfColumn), cellRows - 1);
                Vector2 pos = OffsetFromIndex(index);
                if (direction == EDirection.VERTICAL) pos.y = (pos.y + offset * Math.Sign(pos.y)) * -1;
                else if (direction == EDirection.HORIZONTAL) pos.x = (pos.x + offset * Math.Sign(pos.x)) * -1;
                SetContentOffset(pos);
            }

            public Dictionary<int, GameObject> GetAllCellObjects()
            {
                Dictionary<int, GameObject> cellObjects = new Dictionary<int, GameObject>();
                cellsUsed.Keys.ToList().ForEach((index) =>
                {
                    if (cellNumOfColumn == ONE_COLUMN)
                    {
                        cellObjects.Add(index, cellsUsed[index].GetChild(0).gameObject);
                    }
                    else
                    {
                        for (int i = 0; i < cellNumOfColumn; i++)
                        {
                            int cellIndex = index * cellNumOfColumn + i;
                            if (cellIndex < cellsCount) cellObjects.Add(cellIndex, cellsUsed[index].GetChild(i).GetChild(0).gameObject);
                        }
                    }
                });
                return cellObjects;
            }

            public void CleanAllCellObjects()
            {
                foreach (var cell in cellsUsed)
                {
                    Destroy(cell.Value.gameObject);
                }
                foreach (var cell in cellsFreed)
                {
                    Destroy(cell.gameObject);
                }
                cellsUsed.Clear();
                cellsFreed.Clear();
            }

            public GameObject GetCellObjectByIndex(int index)
            {
                if (!initialized) return null;
                if (index >= 0 && index < cellsCount)
                {
                    if (cellNumOfColumn == ONE_COLUMN)
                    {
                        if (cellsUsed.ContainsKey(index)) return cellsUsed[index].GetChild(0).gameObject;
                    }
                    else
                    {
                        int rowIndex = index / cellNumOfColumn;
                        int columnIndex = index % cellNumOfColumn;
                        if (cellsUsed.ContainsKey(rowIndex))
                        {
                            return cellsUsed[rowIndex].GetChild(columnIndex).GetChild(0).gameObject;
                        }
                    }
                }
                return null;
            }

            public bool IsInDrag()
            {
                return dragFlag;
            }

            public int GetCellsCount()
            {
                return cellsCount;
            }

            public virtual void Update()
            {
                if (!initialized) return;
                bool headerSizeChanged = headerNode != null && headerNode.rect != lastHeaderRectSize;
                bool needRebuildLayout = headerSizeChanged || viewportTransform.rect != lastViewportRectSize;

                if (!needRebuildLayout) return;
                UpdateCellPositions(true);
                UpdateAllCell(headerSizeChanged);
            }

            void OnValueChanged(Vector2 value)
            {
                UpdateAllCell(false, true);
            }

            void InitContentTransform()
            {
                if (direction == EDirection.VERTICAL)
                {
                    int y = vordering == EVerticalFillOrder.TOP_DOWN ? 1 : 0;
                    contentTransform.anchorMin = new Vector2(0, y);
                    contentTransform.anchorMax = new Vector2(1, y);
                    contentTransform.pivot = new Vector2(0, y);
                }
                else if (direction == EDirection.HORIZONTAL)
                {
                    int x = vordering == EVerticalFillOrder.TOP_DOWN ? 0 : 1;
                    contentTransform.anchorMin = new Vector2(x, 0);
                    contentTransform.anchorMax = new Vector2(x, 1);
                    contentTransform.pivot = new Vector2(x, 1);
                }
                // 坐标重置回原点
                contentTransform.offsetMin = Vector2.zero;
                contentTransform.offsetMax = Vector2.zero;
            }

            protected virtual void UpdateCellPositions(bool keepOffset = false)
            {
                lastHeaderRectSize = headerNode == null ? Rect.zero : headerNode.rect;
                cellPositions.Clear();
                cellPositions.Capacity = cellRows + 1;

                float currentPos = 0;
                int sign = ((direction == EDirection.VERTICAL) && (vordering == EVerticalFillOrder.BOTTOM_UP)) || (direction == EDirection.HORIZONTAL) && (vordering == EVerticalFillOrder.TOP_DOWN) ? 1 : -1;

                // 计算 padding
                if (direction == EDirection.VERTICAL) currentPos = padding.top + lastHeaderRectSize.height;
                else if (direction == EDirection.HORIZONTAL) currentPos = padding.left + lastHeaderRectSize.width;

                // 计算加上 spacing 后的 cell 位置
                for (int i = 0; i < cellRows; i++)
                {
                    cellPositions.Insert(i, currentPos * sign);
                    cellPositions[i] = currentPos * sign;
                    Vector2 size = cellSizeForIndexDelegate?.Invoke(i) ?? defaultCellSize;
                    if (direction == EDirection.VERTICAL) currentPos += size.y + spacing;
                    else if (direction == EDirection.HORIZONTAL) currentPos += size.x + spacing;
                }

                // 计算 padding
                if (direction == EDirection.VERTICAL) currentPos += padding.bottom;
                else if (direction == EDirection.HORIZONTAL) currentPos += padding.right;

                // 需要移除最后一个 cell 多余的 spacing
                cellPositions.Insert(cellRows, (currentPos - spacing) * sign);

                // 设置ContentSize
                float contentLength = Math.Abs(cellPositions.Last());
                if (direction == EDirection.VERTICAL) contentTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentLength);
                else if (direction == EDirection.HORIZONTAL) contentTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentLength);

                // 设置ContentPosition
                var anchoredPosition = keepOffset? GetContentOffset() : Vector2.zero;
                if (centerContentIfNotFillViewport)
                {
                    float viewportLength = direction == EDirection.VERTICAL ? viewportTransform.rect.height : viewportTransform.rect.width;
                    centerContentActive = contentLength <= viewportLength;
                    scrollRect.enabled = !centerContentActive;
                    if (centerContentActive)
                    {
                        var offset = (viewportLength - contentLength) / 2 * sign;
                        if (direction == EDirection.HORIZONTAL) anchoredPosition = new Vector2(offset, 0);
                        else if (direction == EDirection.VERTICAL) anchoredPosition = new Vector2(0, offset);
                    }
                }
                contentTransform.anchoredPosition = anchoredPosition;
            }

            void UpdateAllCell(bool force = false, bool ignoreExist = false)
            {
                lastViewportRectSize = viewportTransform.rect;

                if (lastViewportRectSize.width <= 0 || lastViewportRectSize.height <= 0) return;

                if (emptyTipsNode != null) emptyTipsNode.gameObject.SetActive(cellsCount == 0);

                int sign = ((direction == EDirection.VERTICAL) && (vordering == EVerticalFillOrder.BOTTOM_UP)) || (direction == EDirection.HORIZONTAL) && (vordering == EVerticalFillOrder.TOP_DOWN) ? -1 : 1;
                Vector2 offset = GetContentOffset();
                offset.x = (offset.x * sign < 0) ? 0 : offset.x;
                offset.y = (offset.y * sign < 0) ? 0 : offset.y;

                int startIndex = IndexFromOffset(offset, true);
                startIndex = startIndex == CC_INVALID_INDEX ? 0 : startIndex;

                // 第一次初始化时 rect 大小可能不对，取屏幕宽高
                offset.x += lastViewportRectSize.width * sign;
                offset.y += lastViewportRectSize.height * sign;

                int endIndex = IndexFromOffset(offset, false);
                endIndex = endIndex == CC_INVALID_INDEX ? cellRows - 1 : endIndex;

                // Debug.Log("startIndex:" + startIndex + " endIndex:" + endIndex);

                // 把不在画面内的cell移除
                cellsUsed.Keys.ToList().ForEach((index) =>
                {
                    if (index < startIndex || index > endIndex || force) FreeCell(index);
                });

                var updatedPrefabs = new Dictionary<int, GameObject>();
                for (int i = startIndex; i <= endIndex; i++)
                {
                    if (ignoreExist && cellsUsed.ContainsKey(i)) continue;
                    UpdateCellAtRowIndex(i, updatedPrefabs);
                }

                foreach (var item in updatedPrefabs)
				{
					OnCellUpdate.Invoke(item.Key, item.Value);
				}

                int startCellIndex = startIndex * cellNumOfColumn;
                int endCellIndex = Math.Min(cellsCount, (endIndex + 1) * cellNumOfColumn - 1);
                contentMoveDelegate?.Invoke(startCellIndex, endCellIndex);

                if (startHintNode != null) startHintNode.gameObject.SetActive(startIndex > 0);
                if (endHintNode != null) endHintNode.gameObject.SetActive(endIndex < cellRows - 1);
            }

            void UpdateCellAtRowIndex(int index, Dictionary<int, GameObject> updatedPrefabs)
            {
                if (index < 0 || index >= cellsCount) return;
                RectTransform cellRowTrans = DequeueCellRow(index, out bool isExist);
                Vector2 pos = OffsetFromIndex(index);
                cellRowTrans.anchoredPosition = pos;

                if (cellNumOfColumn == ONE_COLUMN)
                {
                    var curCellSize = cellSizeForIndexDelegate?.Invoke(index) ?? defaultCellSize;
                    cellRowTrans.sizeDelta = curCellSize;

                    GameObject prefab = cellRowTrans.GetChild(0).gameObject;
                    RectTransform prefabTrans = prefab.GetComponent<RectTransform>();
                    prefabTrans.sizeDelta = curCellSize;
                    prefabTrans.anchoredPosition = Vector2.zero;
                    updatedPrefabs[index] = prefab;
                }
                else
                {
                    for (int i = 0; i < cellNumOfColumn; i++)
                    {
                        Transform cell = cellRowTrans.GetChild(i);
                        int cellIndex = index * cellNumOfColumn + i;
                        Vector2 curCellSize = cellSizeForIndexDelegate?.Invoke(cellIndex) ?? defaultCellSize;
                        cell.GetComponent<RectTransform>().sizeDelta = curCellSize;

                        GameObject prefab = cell.GetChild(0).gameObject;
                        RectTransform prefabTrans = prefab.GetComponent<RectTransform>();
                        prefabTrans.sizeDelta = curCellSize;
                        prefabTrans.anchoredPosition = Vector2.zero;

                        cell.gameObject.SetActive(!customRowLayout || cellIndex < cellsCount);
                        prefab.SetActive(cellIndex < cellsCount);

                        if (cellIndex < cellsCount) updatedPrefabs[cellIndex] = prefab;
                    }
                }
            }

            Vector2 GetContentOffset()
            {
                return contentTransform.anchoredPosition;
            }

            protected virtual void SetContentOffset(Vector2 offset)
            {
                float contentLength = Math.Abs(cellPositions.Last());
                if (direction == EDirection.VERTICAL) offset.y = Math.Clamp(Math.Abs(offset.y), 0, Math.Max(0, contentLength - viewportTransform.rect.height)) * Math.Sign(offset.y);
                else if (direction == EDirection.HORIZONTAL) offset.x = Math.Clamp(Math.Abs(offset.x), 0, Math.Max(0, contentLength - viewportTransform.rect.width)) * Math.Sign(offset.x);
                if (Vector2.Distance(contentTransform.anchoredPosition, offset) < 0.1f) return;
                contentTransform.anchoredPosition = offset;
                UpdateAllCell();
            }

            Vector2 OffsetFromIndex(int index)
            {
                if (cellPositions.Count <= 0) return Vector2.zero;
                Vector2 offset = Vector2.zero;
                if (direction == EDirection.VERTICAL) offset.y = cellPositions[index];
                else if (direction == EDirection.HORIZONTAL) offset.x = cellPositions[index];
                return offset;
            }

            int IndexFromOffset(Vector2 offset, bool start)
            {
                if (cellPositions.Count <= 0) return CC_INVALID_INDEX;

                // 使用二分法查找 offset 处于 cellPositions 数组中的位置
                int low = 0;
                int high = cellRows - 1;
                float searchPos = CC_INVALID_INDEX;
                if (direction == EDirection.VERTICAL) searchPos = Math.Abs(offset.y);
                else if (direction == EDirection.HORIZONTAL) searchPos = Math.Abs(offset.x);

                float cellHalfSize = 0;
                if (!showItemWhenCenterNotInRect)
                {
                    if (direction == EDirection.VERTICAL) cellHalfSize = defaultCellSize.y / 2;
                    else if (direction == EDirection.HORIZONTAL) cellHalfSize = defaultCellSize.x / 2;
                }

                while (high >= low)
                {
                    int index = low + (high - low) / 2;
                    float cellStart = Math.Abs(cellPositions[index]);
                    float cellEnd = Math.Abs(cellPositions[index + 1]);
                    if (searchPos >= cellStart && searchPos <= cellEnd)
                    {
                        if (!showItemWhenCenterNotInRect)
                        {
                            if (start && searchPos > cellEnd - cellHalfSize) return index + 1;
                            else if (!start && searchPos < cellStart + cellHalfSize) return index - 1;
                            else return index;
                        }
                        else
                        {
                            return index;
                        }
                    }
                    else if (searchPos < cellStart) high = index - 1;
                    else if (searchPos > cellEnd) low = index + 1;
                }

                return CC_INVALID_INDEX;
            }

            void FreeCell(int index)
            {
                RectTransform cellRow = cellsUsed[index];
                cellsFreed.Enqueue(cellRow);
                cellsUsed.Remove(index);
                cellRow.gameObject.SetActive(false);
            }

            RectTransform DequeueCellRow(int index, out bool isExist)
            {
                RectTransform cellRowTrans;
                isExist = cellsUsed.TryGetValue(index, out cellRowTrans);
                if (!isExist)
                {
                    cellRowTrans = cellsFreed.Count > 0 ? cellsFreed.Dequeue() : CreateCellRow();
                    cellsUsed.Add(index, cellRowTrans);
                    cellRowTrans.gameObject.SetActive(true);
                }
                return cellRowTrans;
            }

            RectTransform CreateCellRow()
            {
                RectTransform cellRowTrans;
                if (cellNumOfColumn == ONE_COLUMN)
                {
                    cellRowTrans = CreateCell(contentTransform);
                    if (direction == EDirection.VERTICAL)
                    {
                        cellRowTrans.pivot = new Vector2(0.5f, vordering == EVerticalFillOrder.TOP_DOWN ? 1 : 0);
                        cellRowTrans.anchorMin = new Vector2(0.5f, vordering == EVerticalFillOrder.TOP_DOWN ? 1 : 0);
                        cellRowTrans.anchorMax = new Vector2(0.5f, vordering == EVerticalFillOrder.TOP_DOWN ? 1 : 0);
                    }
                    else if (direction == EDirection.HORIZONTAL)
                    {
                        cellRowTrans.pivot = new Vector2(vordering == EVerticalFillOrder.TOP_DOWN ? 0 : 1, 0.5f);
                        cellRowTrans.anchorMin = new Vector2(vordering == EVerticalFillOrder.TOP_DOWN ? 0 : 1, 0.5f);
                        cellRowTrans.anchorMax = new Vector2(vordering == EVerticalFillOrder.TOP_DOWN ? 0 : 1, 0.5f);
                    }
                }
                else
                {
                    GameObject go = new GameObject("CellRow", typeof(RectTransform));
                    go.transform.SetParent(contentTransform, false);
                    cellRowTrans = go.GetComponent<RectTransform>();

                    if (direction == EDirection.VERTICAL) go.AddComponent<HorizontalLayoutGroup>();
                    else if (direction == EDirection.HORIZONTAL) go.AddComponent<VerticalLayoutGroup>();

                    if (customRowLayout)
                    {
                        HorizontalOrVerticalLayoutGroup layoutGroup = go.GetComponent<HorizontalOrVerticalLayoutGroup>();
                        layoutGroup.spacing = columnSpacing;
                        layoutGroup.childAlignment = columnAlignment;
                        layoutGroup.childForceExpandWidth = false;
                        layoutGroup.childForceExpandHeight = false;
                        layoutGroup.childControlWidth = false;
                        layoutGroup.childControlHeight = false;
                    }

                    for (int i = 0; i < cellNumOfColumn; i++)
                    {
                        CreateCell(cellRowTrans);
                    }
                    if (direction == EDirection.VERTICAL)
                    {
                        cellRowTrans.pivot = new Vector2(0.5f, vordering == EVerticalFillOrder.TOP_DOWN ? 1 : 0);
                        cellRowTrans.anchorMin = new Vector2(0, vordering == EVerticalFillOrder.TOP_DOWN ? 1 : 0);
                        cellRowTrans.anchorMax = new Vector2(1, vordering == EVerticalFillOrder.TOP_DOWN ? 1 : 0);
                        cellRowTrans.sizeDelta = new Vector2(0, defaultCellSize.y);
                    }
                    else if (direction == EDirection.HORIZONTAL)
                    {
                        cellRowTrans.pivot = new Vector2(vordering == EVerticalFillOrder.TOP_DOWN ? 0 : 1, 0.5f);
                        cellRowTrans.anchorMin = new Vector2(vordering == EVerticalFillOrder.TOP_DOWN ? 0 : 1, 0);
                        cellRowTrans.anchorMax = new Vector2(vordering == EVerticalFillOrder.TOP_DOWN ? 0 : 1, 1);
                        cellRowTrans.sizeDelta = new Vector2(defaultCellSize.x, 0);
                    }
                }

                return cellRowTrans;
            }

            RectTransform CreateCell(Transform parent)
            {
                GameObject cell = new GameObject("Cell", typeof(RectTransform));
                RectTransform cellTrans = cell.GetComponent<RectTransform>();
                cellTrans.SetParent(parent, false);
                cellTrans.pivot = VECTOR_2_CENTER;
                cellTrans.anchorMin = VECTOR_2_CENTER;
                cellTrans.anchorMax = VECTOR_2_CENTER;
                cellTrans.anchoredPosition = VECTOR_2_CENTER;
                // cellTrans.sizeDelta = cellSize;

                GameObject prefab = Instantiate(cellPrefab, cellTrans);
                RectTransform prefabTrans = prefab.GetComponent<RectTransform>();
                prefabTrans.pivot = VECTOR_2_CENTER;
                prefabTrans.anchorMin = VECTOR_2_CENTER;
                prefabTrans.anchorMax = VECTOR_2_CENTER;
                prefabTrans.anchoredPosition = VECTOR_2_CENTER;
                // prefabTrans.sizeDelta = cellSize;

                prefab.SetActive(true);
                OnCellInit.Invoke(prefab);
                return cellTrans;
            }
        }
    }
}