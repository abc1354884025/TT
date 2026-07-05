using System.Collections.Generic;

    /// <summary>关卡选择 ViewModel</summary>
    public class LevelSelectViewModel : ObservableObject
    {
        public BindableProperty<string> PuzzleTitle = new BindableProperty<string>("");
        public PuzzleType CurrentPuzzleType { get; private set; }
        public List<LevelSelectItemData> Levels { get; private set; } = new List<LevelSelectItemData>();

        /// <summary>刷新关卡列表数据（通关后回来需要重新拉取存档状态）</summary>
        public void RefreshLevels()
        {
            Levels = LevelDatabase.GetLevelSelectItems(CurrentPuzzleType);
        }

        public void Init(PuzzleType type)
        {
            CurrentPuzzleType = type;
            PuzzleTitle.Value = LevelDatabase.GetPuzzleName(type);
            Levels = LevelDatabase.GetLevelSelectItems(type);
        }

        public PuzzleLevelData GetLevelData(int levelIndex)
        {
            return LevelDatabase.CreateLevel(CurrentPuzzleType, levelIndex);
        }

        public string GetPuzzlePanelName()
        {
            switch (CurrentPuzzleType)
            {
                case PuzzleType.Sudoku: return "SudokuPanel";
                case PuzzleType.Nurikabe: return "NurikabePanel";
                case PuzzleType.NumberLink: return "NumberLinkPanel";
                case PuzzleType.HashiBridge: return "HashiBridgePanel";
                default: return "";
            }
        }

        public override void Dispose()
        {
            PuzzleTitle.Clear();
            base.Dispose();
        }
    }
