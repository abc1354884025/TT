using System.Collections.Generic;
using UnityEngine;

    /// <summary>数桥 ViewModel</summary>
    public class HashiBridgeViewModel : PuzzleGameViewModel
    {
        public HashiBridgeGrid Grid { get; private set; }
        private HashiBridgeRuleEngine _ruleEngine;

        private Vector2Int _dragStartIsland = new Vector2Int(-1, -1);

        public Vector2Int DragStartIsland => _dragStartIsland;

        public HashiBridgeViewModel() : base()
        {
            PuzzleType = PuzzleType.HashiBridge;
        }

        public void InitFromLevel(HashiBridgeLevelData data)
        {
            LevelName.Value = data.DisplayName;
            LevelId = data.LevelId;
            LevelIndex = data.LevelIndex;
            Grid = new HashiBridgeGrid(data.GridWidth, data.GridHeight);

            var islands = new (Vector2Int pos, int required)[data.Islands.Count];
            for (int i = 0; i < data.Islands.Count; i++)
                islands[i] = (data.Islands[i].Position, data.Islands[i].RequiredBridges);
            Grid.Initialize(islands);
            _ruleEngine = new HashiBridgeRuleEngine(Grid);
            RuleEngine = _ruleEngine;
        }

        public override void ProcessMove(PuzzleMove move)
        {
            switch (move.ActionType)
            {
                case InputActionType.DragStart:
                    OnDragStart(move.Position);
                    break;
                case InputActionType.DragEnd:
                    if (move.SecondaryPos.HasValue)
                        OnDragEnd(move.Position, move.SecondaryPos.Value);
                    else
                        OnTap(move.Position);
                    break;
                case InputActionType.Tap:
                    OnTap(move.Position);
                    break;
            }
        }

        private void OnDragStart(Vector2Int pos)
        {
            var cell = Grid.Grid[pos];
            if (cell.Type == HashiCellType.Island)
                _dragStartIsland = pos;
        }

        private void OnDragEnd(Vector2Int from, Vector2Int to)
        {
            var fromCell = Grid.Grid[from];
            var toCell = Grid.Grid[to];

            if (fromCell.Type != HashiCellType.Island || toCell.Type != HashiCellType.Island)
            {
                _dragStartIsland = new Vector2Int(-1, -1);
                return;
            }

            // 检查是否可以建桥
            if (!Grid.CanBridge(from, to))
            {
                _dragStartIsland = new Vector2Int(-1, -1);
                return;
            }

            int currentCount = Grid.GetBridgeCount(from, to);
            int delta = currentCount >= 2 ? -2 : 1; // 循环: 0→1→2→0

            int actualDelta = Grid.ModifyBridge(from, to, delta);
            if (actualDelta != 0)
            {
                IncrementMove();
                MoveHistory.Push(new HashiBridgeMove
                {
                    ActionType = InputActionType.DragEnd,
                    Position = from,
                    SecondaryPos = to,
                    Value = actualDelta
                });
            }

            _dragStartIsland = new Vector2Int(-1, -1);
        }

        private void OnTap(Vector2Int pos)
        {
            // 点击岛上可以查看连接状态（MVP 无额外操作）
            _dragStartIsland = new Vector2Int(-1, -1);
        }

        public override void Undo()
        {
            if (MoveHistory.Count == 0) return;
            var move = MoveHistory.Pop();
            if (move is HashiBridgeMove hMove && hMove.SecondaryPos.HasValue)
            {
                Grid.ModifyBridge(hMove.Position, hMove.SecondaryPos.Value, -hMove.Value);
                MoveCount.Value--;
            }
        }

        public override void RequestHint()
        {
            Debug.Log("[HashiBridgeVM] 提示功能暂未实现");
        }

        public override void SaveState()
        {
            if (string.IsNullOrEmpty(LevelId)) return;
            var save = new HashiBridgeSaveData();
            save.FromGrid(Grid);
            SaveManager.SavePuzzleState(PuzzleType, LevelId, save.ToJson(),
                IsPuzzleSolved.Value, ElapsedTime, MoveCount.Value);
        }

        public override void LoadState()
        {
            var entry = SaveManager.GetEntry(PuzzleType, LevelId);
            if (entry != null && !string.IsNullOrEmpty(entry.RawSaveState))
            {
                try
                {
                    var save = HashiBridgeSaveData.FromJson(entry.RawSaveState);
                    save?.ToGrid(Grid);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[HashiBridgeVM] 加载存档失败: {e.Message}");
                }
            }
        }
    }
