using System.Collections.Generic;
using UnityEngine;

    /// <summary>数墙 ViewModel</summary>
    public class NurikabeViewModel : PuzzleGameViewModel
    {
        public NurikabeGrid Grid { get; private set; }
        private NurikabeRuleEngine _ruleEngine;

        public NurikabeViewModel() : base()
        {
            PuzzleType = PuzzleType.Nurikabe;
        }

        public void InitFromLevel(NurikabeLevelData data)
        {
            LevelName.Value = data.DisplayName;
            LevelId = data.LevelId;
            LevelIndex = data.LevelIndex;
            Grid = new NurikabeGrid(data.GridWidth, data.GridHeight);
            Grid.Initialize(data.NumberedCells);
            _ruleEngine = new NurikabeRuleEngine(Grid);
            RuleEngine = _ruleEngine;
        }

        public override void ProcessMove(PuzzleMove move)
        {
            if (move.ActionType == InputActionType.Tap)
            {
                int x = move.Position.x;
                int y = move.Position.y;
                if (!Grid.Grid.InBounds(x, y)) return;

                var cell = Grid.Grid[x, y];
                if (cell.IsLocked) return;

                NurikabeCellState oldState = cell.State;
                if (Grid.Toggle(x, y))
                {
                    IncrementMove();
                    MoveHistory.Push(new NurikabeMove
                    {
                        ActionType = InputActionType.Tap,
                        Position = new Vector2Int(x, y),
                        Value = (int)cell.State,
                        OldValue = (int)oldState
                    });
                }
            }
        }

        public override void Undo()
        {
            if (MoveHistory.Count == 0) return;
            var move = MoveHistory.Pop();
            if (move is NurikabeMove nMove)
            {
                var cell = Grid.Grid[nMove.Position];
                cell.State = nMove.PreviousState;
                MoveCount.Value--;
            }
        }

        public override void RequestHint()
        {
            // 简化版提示：找一个没有岛屿归属的白色格子，暂时不需实现完整求解器
            Debug.Log("[NurikabeVM] 提示功能暂未实现");
        }

        public override void SaveState()
        {
            if (string.IsNullOrEmpty(LevelId)) return;
            var save = new NurikabeSaveData();
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
                    var save = NurikabeSaveData.FromJson(entry.RawSaveState);
                    save?.ToGrid(Grid);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[NurikabeVM] 加载存档失败: {e.Message}");
                }
            }
        }
    }
