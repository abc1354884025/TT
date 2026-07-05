using System;
using System.Collections.Generic;
using UnityEngine;

    /// <summary>数桥格子数据</summary>
    [Serializable]
    public class HashiCell : GridCellData
    {
        /// <summary>格子类型</summary>
        public HashiCellType Type = HashiCellType.Sea;

        /// <summary>岛屿所需桥的总数（仅岛屿有效）</summary>
        public int IslandValue;

        /// <summary>当前已连接的桥数量</summary>
        public int CurrentBridgeCount;

        /// <summary>是否已达到所需桥数量</summary>
        public bool IsComplete => Type == HashiCellType.Island && CurrentBridgeCount >= IslandValue;

        public override void ResetToDefault()
        {
            if (!IsLocked && Type == HashiCellType.Sea)
            {
                // Sea cells are always empty
            }
            CurrentBridgeCount = 0;
        }

        public override object Clone()
        {
            return new HashiCell
            {
                Position = Position,
                IsLocked = IsLocked,
                Type = Type,
                IslandValue = IslandValue,
                CurrentBridgeCount = CurrentBridgeCount
            };
        }
    }

    /// <summary>数桥棋盘</summary>
    public class HashiBridgeGrid
    {
        public PuzzleGrid<HashiCell> Grid { get; private set; }

        /// <summary>(岛A坐标, 岛B坐标) -> 桥数量 (0-2)</summary>
        public Dictionary<(Vector2Int, Vector2Int), int> Bridges = new Dictionary<(Vector2Int, Vector2Int), int>();

        public HashiBridgeGrid(int width, int height)
        {
            Grid = new PuzzleGrid<HashiCell>(width, height);
        }

        /// <summary>根据关卡数据初始化（放置岛屿）</summary>
        public void Initialize((Vector2Int pos, int requiredBridges)[] islands)
        {
            foreach (var (pos, required) in islands)
            {
                var cell = Grid[pos];
                cell.Type = HashiCellType.Island;
                cell.IslandValue = required;
                cell.IsLocked = true;
            }
        }

        /// <summary>在两个岛之间添加/修改桥。
        /// 返回桥数量的变化（正=添加，负=移除，0=无效操作）。</summary>
        public int ModifyBridge(Vector2Int a, Vector2Int b, int delta)
        {
            // 规范化键
            var key = a.x < b.x || (a.x == b.x && a.y < b.y) ? (a, b) : (b, a);

            if (!Bridges.TryGetValue(key, out int current))
                current = 0;

            int newCount = Mathf.Clamp(current + delta, 0, 2);
            if (newCount == current) return 0;

            int actualDelta = newCount - current;

            if (newCount == 0)
                Bridges.Remove(key);
            else
                Bridges[key] = newCount;

            // 更新两端岛屿的当前桥数量
            Grid[a].CurrentBridgeCount += actualDelta;
            Grid[b].CurrentBridgeCount += actualDelta;

            return actualDelta;
        }

        /// <summary>获取两个岛之间的当前桥数量</summary>
        public int GetBridgeCount(Vector2Int a, Vector2Int b)
        {
            var key = a.x < b.x || (a.x == b.x && a.y < b.y) ? (a, b) : (b, a);
            return Bridges.TryGetValue(key, out int count) ? count : 0;
        }

        /// <summary>检查两个岛之间是否可以直接建桥（同行或同列，且中间没有岛屿阻隔）</summary>
        public bool CanBridge(Vector2Int a, Vector2Int b)
        {
            if (a == b) return false;
            var cellA = Grid[a];
            var cellB = Grid[b];
            if (cellA.Type != HashiCellType.Island || cellB.Type != HashiCellType.Island) return false;

            // 必须在同一行或同一列
            if (a.x != b.x && a.y != b.y) return false;

            // 检查中间是否有其他岛屿
            var between = Grid.GetCellsBetween(a, b);
            foreach (var pos in between)
            {
                if (Grid[pos].Type == HashiCellType.Island) return false;
            }

            return true;
        }

        /// <summary>检查所有岛屿是否连通（BFS）</summary>
        public bool AreAllIslandsConnected()
        {
            var islands = GetIslands();
            if (islands.Count <= 1) return true;

            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(islands[0]);
            visited.Add(islands[0]);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var target in islands)
                {
                    if (!visited.Contains(target) && GetBridgeCount(current, target) > 0)
                    {
                        visited.Add(target);
                        queue.Enqueue(target);
                    }
                }
            }

            return visited.Count == islands.Count;
        }

        /// <summary>获取所有岛屿坐标</summary>
        public List<Vector2Int> GetIslands()
        {
            var islands = new List<Vector2Int>();
            foreach (var pos in Grid.AllPositions())
                if (Grid[pos].Type == HashiCellType.Island)
                    islands.Add(pos);
            return islands;
        }
    }
