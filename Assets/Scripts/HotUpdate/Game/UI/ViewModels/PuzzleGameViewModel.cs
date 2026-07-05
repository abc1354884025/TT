using System.Collections.Generic;

    /// <summary>
    /// 益智游戏 ViewModel 抽象基类。所有游戏的 ViewModel 继承此类。
    /// </summary>
    public abstract class PuzzleGameViewModel : ObservableObject
    {
        // --- 绑定属性 ---
        public BindableProperty<string> LevelName = new BindableProperty<string>("");
        public BindableProperty<string> TimerDisplay = new BindableProperty<string>("00:00");
        public BindableProperty<int> MoveCount = new BindableProperty<int>(0);
        public BindableProperty<bool> IsPuzzleSolved = new BindableProperty<bool>(false);

        // --- 内部状态 ---
        public PuzzleType PuzzleType { get; set; }
        public string LevelId { get; set; }
        public int LevelIndex { get; set; }
        public float ElapsedTime { get; set; }
        protected Stack<PuzzleMove> MoveHistory = new Stack<PuzzleMove>();
        protected IPuzzleRuleEngine RuleEngine;

        /// <summary>处理玩家操作（由 Panel 转发）</summary>
        public abstract void ProcessMove(PuzzleMove move);

        /// <summary>撤销上一步</summary>
        public abstract void Undo();

        /// <summary>请求提示</summary>
        public abstract void RequestHint();

        /// <summary>检查当前是否为有效解</summary>
        public virtual bool CheckSolution()
        {
            if (RuleEngine != null)
            {
                bool solved = RuleEngine.IsSolutionValid();
                if (solved) IsPuzzleSolved.Value = true;
                return solved;
            }
            return false;
        }

        /// <summary>更新计时器（由 Panel 的 Update 或协程驱动）</summary>
        public void UpdateTimer(float deltaTime)
        {
            if (IsPuzzleSolved.Value) return;
            ElapsedTime += deltaTime;
            int min = (int)(ElapsedTime / 60);
            int sec = (int)(ElapsedTime % 60);
            TimerDisplay.Value = $"{min:D2}:{sec:D2}";
        }

        /// <summary>增加步数</summary>
        protected void IncrementMove()
        {
            MoveCount.Value++;
        }

        /// <summary>保存当前状态</summary>
        public abstract void SaveState();

        /// <summary>加载已保存的状态</summary>
        public abstract void LoadState();

        public override void Dispose()
        {
            LevelName.Clear();
            TimerDisplay.Clear();
            MoveCount.Clear();
            IsPuzzleSolved.Clear();
            MoveHistory.Clear();
            base.Dispose();
        }
    }
