using System.Collections.Generic;
using System.Text;
using UnityEngine;

    /// <summary>数独 ViewModel</summary>
    public class SudokuViewModel : PuzzleGameViewModel
    {
        public SudokuGrid Grid { get; private set; }
        private SudokuRuleEngine _ruleEngine;
        private Vector2Int _selectedCell = new Vector2Int(-1, -1);

        /// <summary>当前选中的单元格（-1,-1 表示未选中）</summary>
        public Vector2Int SelectedCell => _selectedCell;

        public SudokuViewModel() : base()
        {
            PuzzleType = PuzzleType.Sudoku;
            Grid = new SudokuGrid();
            _ruleEngine = new SudokuRuleEngine(Grid);
            RuleEngine = _ruleEngine;
        }

        public void InitFromLevel(SudokuLevelData data)
        {
            LevelName.Value = data.DisplayName;
            LevelId = data.LevelId;
            LevelIndex = data.LevelIndex;
            Grid.Initialize(data.Clues);
        }

        /// <summary>选中一个格子</summary>
        public void SelectCell(int x, int y)
        {
            if (!Grid.Grid.InBounds(x, y)) return;
            _selectedCell = new Vector2Int(x, y);
        }

        /// <summary>在选中的格子中填入数字</summary>
        public void SetDigit(int digit)
        {
            if (_selectedCell.x < 0) return;
            var cell = Grid.Grid[_selectedCell];
            if (cell.IsClue) return;

            int oldValue = cell.Value;
            if (!Grid.SetValue(_selectedCell.x, _selectedCell.y, digit))
                return;

            IncrementMove();
            MoveHistory.Push(new SudokuMove
            {
                ActionType = InputActionType.Tap,
                Position = _selectedCell,
                Value = digit,
                OldValue = oldValue
            });

            // 更新错误高亮
            UpdateErrors();
        }

        public override void ProcessMove(PuzzleMove move)
        {
            if (move is SudokuMove sudokuMove)
            {
                SelectCell(sudokuMove.Position.x, sudokuMove.Position.y);
                SetDigit(sudokuMove.Digit);
            }
            else if (move.ActionType == InputActionType.Tap)
            {
                SelectCell(move.Position.x, move.Position.y);
            }
        }

        public override void Undo()
        {
            if (MoveHistory.Count == 0) return;
            var move = MoveHistory.Pop();
            if (move is SudokuMove sMove)
            {
                Grid.SetValue(sMove.Position.x, sMove.Position.y, sMove.OldValue);
                MoveCount.Value--;
            }
            UpdateErrors();
        }

        public override void RequestHint()
        {
            // 找一个空白格，填入一个正确数字（简化版：填第一个合理值）
            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 9; y++)
                {
                    var cell = Grid.Grid[x, y];
                    if (cell.IsClue || cell.Value > 0) continue;

                    for (int d = 1; d <= 9; d++)
                    {
                        if (!HasConflict(x, y, d))
                        {
                            Grid.SetValue(x, y, d);
                            IncrementMove();
                            UpdateErrors();
                            return;
                        }
                    }
                }
        }

        private void UpdateErrors()
        {
            var violations = _ruleEngine.GetViolations();
            for (int x = 0; x < 9; x++)
                for (int y = 0; y < 9; y++)
                    Grid.Grid[x, y].IsError = violations.Contains(new Vector2Int(x, y));
        }

        private bool HasConflict(int x, int y, int digit)
        {
            var g = Grid.Grid;
            for (int i = 0; i < 9; i++)
            {
                if (i != x && g[i, y].Value == digit) return true;
                if (i != y && g[x, i].Value == digit) return true;
            }
            int bx = x / 3 * 3, by = y / 3 * 3;
            for (int dx = 0; dx < 3; dx++)
                for (int dy = 0; dy < 3; dy++)
                {
                    int cx = bx + dx, cy = by + dy;
                    if ((cx != x || cy != y) && g[cx, cy].Value == digit) return true;
                }
            return false;
        }

        public override void SaveState()
        {
            if (string.IsNullOrEmpty(LevelId)) return;
            var save = new SudokuSaveData();
            save.FromGrid(Grid.GetPlayerValues());
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
                    var save = SudokuSaveData.FromJson(entry.RawSaveState);
                    if (save != null)
                    {
                        var values = save.ToGrid();
                        for (int x = 0; x < 9; x++)
                            for (int y = 0; y < 9; y++)
                                if (values[x, y] > 0 && !Grid.Grid[x, y].IsClue)
                                    Grid.SetValue(x, y, values[x, y]);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SudokuVM] 加载存档失败: {e.Message}");
                }
            }
        }
    }
