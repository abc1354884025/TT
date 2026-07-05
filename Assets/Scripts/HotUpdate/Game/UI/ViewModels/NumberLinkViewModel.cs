using System.Collections.Generic;
using UnityEngine;

    /// <summary>数回 ViewModel</summary>
    public class NumberLinkViewModel : PuzzleGameViewModel
    {
        public NumberLinkGrid Grid { get; private set; }
        private NumberLinkRuleEngine _ruleEngine;

        private int _currentDrawValue = -1;      // 当前正在绘制的数字值
        private Vector2Int _currentEndpoint = new Vector2Int(-1, -1); // 起始端点

        public int CurrentDrawValue => _currentDrawValue;

        public NumberLinkViewModel() : base()
        {
            PuzzleType = PuzzleType.NumberLink;
        }

        public void InitFromLevel(NumberLinkLevelData data)
        {
            LevelName.Value = data.DisplayName;
            LevelId = data.LevelId;
            LevelIndex = data.LevelIndex;
            Grid = new NumberLinkGrid(data.GridWidth, data.GridHeight);

            var pairs = new (int, Vector2Int, Vector2Int)[data.Pairs.Count];
            for (int i = 0; i < data.Pairs.Count; i++)
                pairs[i] = (data.Pairs[i].Value, data.Pairs[i].Pos1, data.Pairs[i].Pos2);
            Grid.Initialize(pairs);
            _ruleEngine = new NumberLinkRuleEngine(Grid);
            RuleEngine = _ruleEngine;
        }

        public override void ProcessMove(PuzzleMove move)
        {
            int x = move.Position.x;
            int y = move.Position.y;
            if (!Grid.Grid.InBounds(x, y)) return;

            switch (move.ActionType)
            {
                case InputActionType.DragStart:
                    OnDragStart(new Vector2Int(x, y));
                    break;
                case InputActionType.DragEnter:
                    OnDragEnter(new Vector2Int(x, y));
                    break;
                case InputActionType.DragEnd:
                    OnDragEnd(new Vector2Int(x, y));
                    break;
            }
        }

        private void OnDragStart(Vector2Int pos)
        {
            var cell = Grid.Grid[pos];
            if (cell.IsEndpoint)
            {
                _currentDrawValue = cell.NumberValue;
                _currentEndpoint = pos;
            }
        }

        private void OnDragEnter(Vector2Int pos)
        {
            if (_currentDrawValue <= 0) return;
            var cell = Grid.Grid[pos];

            // 如果到达了同数字的端点，完成连接
            if (cell.IsEndpoint && cell.NumberValue == _currentDrawValue && pos != _currentEndpoint)
            {
                OnDragEnd(pos);
                return;
            }

            // 如果格子已被占用，且不是当前路径的一部分
            if (cell.NumberValue > 0 && cell.NumberValue != _currentDrawValue)
                return;

            // 添加到路径（如果尚未在此路径中）
            if (cell.NumberValue != _currentDrawValue)
            {
                Grid.AddToPath(pos, _currentDrawValue, PuzzleDirection.None, PuzzleDirection.None);
                IncrementMove();
                MoveHistory.Push(new NumberLinkMove
                {
                    ActionType = InputActionType.DragEnter,
                    Position = pos,
                    Value = _currentDrawValue
                });
            }
        }

        private void OnDragEnd(Vector2Int pos)
        {
            // 如果结束位置是同数字的端点，完成连接
            _currentDrawValue = -1;
            _currentEndpoint = new Vector2Int(-1, -1);
        }

        public override void Undo()
        {
            if (MoveHistory.Count == 0) return;
            var move = MoveHistory.Pop();
            if (move is NumberLinkMove nlMove)
            {
                Grid.RemoveFromPath(nlMove.Position, nlMove.Value);
                MoveCount.Value--;
            }
        }

        public override void RequestHint()
        {
            Debug.Log("[NumberLinkVM] 提示功能暂未实现");
        }

        public override void SaveState()
        {
            if (string.IsNullOrEmpty(LevelId)) return;
            var save = new NumberLinkSaveData();
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
                    var save = NumberLinkSaveData.FromJson(entry.RawSaveState);
                    save?.ToGrid(Grid);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[NumberLinkVM] 加载存档失败: {e.Message}");
                }
            }
        }
    }
