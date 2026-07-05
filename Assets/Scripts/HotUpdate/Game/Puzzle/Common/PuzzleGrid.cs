using System.Collections.Generic;
using UnityEngine;

    /// <summary>
    /// 泛型 2D 网格数据容器。IL2CPP 安全（纯泛型，不使用反射）。
    /// </summary>
    public class PuzzleGrid<T> where T : GridCellData, new()
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public T[,] Cells { get; private set; }

        public PuzzleGrid(int width, int height)
        {
            Width = width;
            Height = height;
            Cells = new T[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    Cells[x, y] = new T();
                    Cells[x, y].Position = new Vector2Int(x, y);
                }
        }

        public T this[int x, int y]
        {
            get => Cells[x, y];
            set => Cells[x, y] = value;
        }

        public T this[Vector2Int pos]
        {
            get => Cells[pos.x, pos.y];
            set => Cells[pos.x, pos.y] = value;
        }

        public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        public bool InBounds(Vector2Int pos) => InBounds(pos.x, pos.y);

        /// <summary>获取四方向的相邻格子坐标（上下左右）</summary>
        public IEnumerable<Vector2Int> GetNeighbors(int x, int y)
        {
            if (x > 0) yield return new Vector2Int(x - 1, y);
            if (x < Width - 1) yield return new Vector2Int(x + 1, y);
            if (y > 0) yield return new Vector2Int(x, y - 1);
            if (y < Height - 1) yield return new Vector2Int(x, y + 1);
        }

        public IEnumerable<Vector2Int> GetNeighbors(Vector2Int pos) => GetNeighbors(pos.x, pos.y);

        /// <summary>遍历所有格子的坐标</summary>
        public IEnumerable<Vector2Int> AllPositions()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    yield return new Vector2Int(x, y);
        }

        /// <summary>重置所有格子为默认状态</summary>
        public void Reset()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    Cells[x, y].ResetToDefault();
        }

        /// <summary>克隆整个网格（深拷贝）</summary>
        public PuzzleGrid<T> Clone()
        {
            var clone = new PuzzleGrid<T>(Width, Height);
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    clone.Cells[x, y] = (T)Cells[x, y].Clone();
            return clone;
        }

        /// <summary>获取两点之间直线方向上的所有格子（用于数桥路径检查）</summary>
        public List<Vector2Int> GetCellsBetween(Vector2Int a, Vector2Int b)
        {
            var cells = new List<Vector2Int>();
            if (a.x == b.x)
            {
                // 垂直方向
                int minY = Mathf.Min(a.y, b.y);
                int maxY = Mathf.Max(a.y, b.y);
                for (int y = minY + 1; y < maxY; y++)
                    cells.Add(new Vector2Int(a.x, y));
            }
            else if (a.y == b.y)
            {
                // 水平方向
                int minX = Mathf.Min(a.x, b.x);
                int maxX = Mathf.Max(a.x, b.x);
                for (int x = minX + 1; x < maxX; x++)
                    cells.Add(new Vector2Int(x, a.y));
            }
            return cells;
        }
    }

    /// <summary>棋盘渲染器：管理网格单元格对象池和定位</summary>
    public class PuzzleGridRenderer
    {
        private readonly ObjectPool<GameObject> _cellPool;
        private readonly List<GameObject> _activeCells = new List<GameObject>();
        private readonly RectTransform _gridArea;

        public IReadOnlyList<GameObject> ActiveCells => _activeCells;

        public PuzzleGridRenderer(GameObject cellPrefab, RectTransform gridArea, int maxPoolSize = 200)
        {
            _gridArea = gridArea;
            _cellPool = new ObjectPool<GameObject>(
                createFunc: () => Object.Instantiate(cellPrefab, gridArea),
                onGet: go => { if (go) go.SetActive(true); },
                onRelease: go =>
                {
                    if (!go) return;
                    go.SetActive(false);
                    go.transform.SetParent(gridArea);
                },
                onDestroy: go => { if (go) Object.Destroy(go); },
                maxSize: maxPoolSize
            );
        }

        /// <summary>根据网格尺寸重新布局单元格</summary>
        public void Rebuild(int gridWidth, int gridHeight)
        {
            // 回收所有激活的单元格
            foreach (var cell in _activeCells)
                _cellPool.Release(cell);
            _activeCells.Clear();

            float areaW = _gridArea.rect.width;
            float areaH = _gridArea.rect.height;
            float cellSize = Mathf.Min(areaW / gridWidth, areaH / gridHeight);

            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    var cell = _cellPool.Get();
                    var rt = cell.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(cellSize, cellSize);
                    rt.anchoredPosition = new Vector2(
                        x * cellSize + cellSize * 0.5f,
                        -(y * cellSize + cellSize * 0.5f)
                    );
                    _activeCells.Add(cell);
                }
            }
        }

        /// <summary>获取指定坐标对应的单元格 GameObject</summary>
        public GameObject GetCell(int x, int y, int gridWidth)
        {
            int index = y * gridWidth + x;
            return index < _activeCells.Count ? _activeCells[index] : null;
        }

        /// <summary>清理所有单元格</summary>
        public void Clear()
        {
            foreach (var cell in _activeCells)
                _cellPool.Release(cell);
            _activeCells.Clear();
            _cellPool.Clear();
        }
    }
