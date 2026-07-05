    /// <summary>益智游戏通关结果</summary>
    public struct PuzzleResult
    {
        /// <summary>是否已解出</summary>
        public bool IsSolved;

        /// <summary>总用时（秒）</summary>
        public float ElapsedTime;

        /// <summary>总操作步数</summary>
        public int MoveCount;

        public PuzzleResult(bool solved, float time, int moves)
        {
            IsSolved = solved;
            ElapsedTime = time;
            MoveCount = moves;
        }
    }
