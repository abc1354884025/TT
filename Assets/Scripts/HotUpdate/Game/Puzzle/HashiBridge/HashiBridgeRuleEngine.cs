using System.Collections.Generic;
using UnityEngine;

    /// <summary>数桥规则引擎</summary>
    public class HashiBridgeRuleEngine : IPuzzleRuleEngine
    {
        private readonly HashiBridgeGrid _grid;

        public HashiBridgeRuleEngine(HashiBridgeGrid grid)
        {
            _grid = grid;
        }

        public bool IsSolutionValid()
        {
            return CheckAllBridgeCountsMatch() && _grid.AreAllIslandsConnected();
        }

        public bool IsMoveValid(PuzzleMove move)
        {
            // 操作合法性由 ViewModel 在处理拖拽时判断
            return true;
        }

        public List<Vector2Int> GetViolations()
        {
            var violations = new List<Vector2Int>();

            foreach (var pos in _grid.Grid.AllPositions())
            {
                var cell = _grid.Grid[pos];
                if (cell.Type == HashiCellType.Island)
                {
                    // 标记桥数量不符的岛屿
                    if (cell.CurrentBridgeCount > cell.IslandValue)
                        violations.Add(pos);
                }
            }

            return violations;
        }

        /// <summary>检查每个岛屿的桥数量是否匹配</summary>
        private bool CheckAllBridgeCountsMatch()
        {
            foreach (var pos in _grid.Grid.AllPositions())
            {
                var cell = _grid.Grid[pos];
                if (cell.Type == HashiCellType.Island)
                    if (cell.CurrentBridgeCount != cell.IslandValue)
                        return false;
            }
            return true;
        }
    }
