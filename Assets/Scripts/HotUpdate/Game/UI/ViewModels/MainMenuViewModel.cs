
    /// <summary>主菜单 ViewModel</summary>
    public class MainMenuViewModel : ObservableObject
    {
        public BindableProperty<string> ProgressSummary = new BindableProperty<string>("已完成 0/20");
        public BindableProperty<bool> HasSaveData = new BindableProperty<bool>(false);

        public MainMenuViewModel()
        {
            RefreshProgress();
        }

        public void RefreshProgress()
        {
            SaveManager.Load();
            string summary = SaveManager.Current.GetProgressSummary(LevelDatabase.LEVELS_PER_PUZZLE);
            ProgressSummary.Value = summary;

            HasSaveData.Value = SaveManager.Current.Entries.Count > 0;
        }

        /// <summary>选择某个游戏类型</summary>
        public PuzzleType SelectedPuzzleType { get; private set; }

        public void SelectPuzzle(PuzzleType type)
        {
            SelectedPuzzleType = type;
        }

        public override void Dispose()
        {
            ProgressSummary.Clear();
            HasSaveData.Clear();
            base.Dispose();
        }
    }
