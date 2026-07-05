using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

    /// <summary>
    /// 益智游戏面板抽象基类。所有益智游戏的面板继承此类。
    /// 提供网格渲染、输入处理、计时器、撤销等公共逻辑。
    /// </summary>
    public abstract class PuzzleGamePanel : UIPanel
    {
        [Header("公共控件")]
        [SerializeField] protected PuzzleInfoWidget InfoWidget;
        [SerializeField] protected RectTransform GridArea;
        [SerializeField] protected VictoryPopupWidget VictoryPopup;
        [SerializeField] protected Button BackButton;
        [SerializeField] protected Button UndoButton;
        [SerializeField] protected Button HintButton;
        [SerializeField] protected GridCellWidget CellPrefab;

        protected PuzzleGameViewModel VM;
        protected GridInputHandler InputHandler;
        protected PuzzleGridRenderer GridRenderer;
        protected readonly List<Action> Unbind = new List<Action>();
        protected PuzzleLevelData LevelData;

        private Coroutine _timerCoroutine;

        protected override void OnOpen(object data)
        {
            LevelData = data as PuzzleLevelData;
            if (LevelData == null)
            {
                Debug.LogError($"[{GetType().Name}] 未收到有效的关卡数据");
                UIManager.Instance.Close(this);
                return;
            }

            // 创建或恢复 ViewModel
            VM = CreateViewModel();
            VM.LevelName.Value = LevelData.DisplayName;
            VM.LevelId = LevelData.LevelId;
            VM.LevelIndex = LevelData.LevelIndex;
            VM.PuzzleType = LevelData.PuzzleType;

            // 尝试加载存档
            VM.LoadState();

            // 挂载输入处理
            InputHandler = GridArea.gameObject.GetComponent<GridInputHandler>();
            if (InputHandler == null)
                InputHandler = GridArea.gameObject.AddComponent<GridInputHandler>();
            InputHandler.SetGridSize(LevelData.GridWidth, LevelData.GridHeight);
            InputHandler.SetDragEnabled(EnableDrag());
            InputHandler.OnMove += OnInputMove;

            // 初始化网格渲染器
            if (CellPrefab && GridArea)
            {
                GridRenderer = new PuzzleGridRenderer(CellPrefab.gameObject, GridArea, LevelData.GridWidth * LevelData.GridHeight + 10);
                GridRenderer.Rebuild(LevelData.GridWidth, LevelData.GridHeight);
            }

            // 绑定公共 UI
            BindCommonUI();

            // 初始渲染
            RenderGrid();

            // 启动计时器
            _timerCoroutine = StartCoroutine(TimerRoutine());
        }

        protected override void OnClose()
        {
            // 停止计时器
            if (_timerCoroutine != null)
                StopCoroutine(_timerCoroutine);

            // 保存状态
            VM?.SaveState();

            // 清理输入
            if (InputHandler)
                InputHandler.OnMove -= OnInputMove;

            // 清理网格渲染
            GridRenderer?.Clear();

            // 解绑 UI
            foreach (var u in Unbind) u.Invoke();
            Unbind.Clear();

            // 清理 VM
            VM?.Dispose();
            VM = null;
        }

        // --- 模板方法：子类实现 ---

        /// <summary>创建具体的 ViewModel 实例</summary>
        protected abstract PuzzleGameViewModel CreateViewModel();

        /// <summary>是否启用拖拽模式</summary>
        protected abstract bool EnableDrag();

        /// <summary>渲染/更新网格显示</summary>
        protected abstract void RenderGrid();

        // --- 公共逻辑 ---

        private void OnInputMove(PuzzleMove move)
        {
            VM?.ProcessMove(move);
            RenderGrid();
            VM?.CheckSolution();
        }

        private void BindCommonUI()
        {
            if (InfoWidget)
            {
                InfoWidget.SetLevelName(LevelData.DisplayName);
                InfoWidget.OnBackClicked += OnBackClicked;
                InfoWidget.OnUndoClicked += OnUndoClicked;
                InfoWidget.OnHintClicked += OnHintClicked;
            }

            if (VictoryPopup)
            {
                VictoryPopup.OnNextLevel += OnNextLevel;
                VictoryPopup.OnBackToMenu += OnBackToMenu;
            }

            // 绑定 VM 属性到 UI
            if (VM != null)
            {
                Unbind.Add(VM.TimerDisplay.SubscribeAndRefresh(t =>
                {
                    if (InfoWidget) InfoWidget.SetTimer(t);
                }));
                Unbind.Add(VM.IsPuzzleSolved.SubscribeAndRefresh(solved =>
                {
                    if (solved && VictoryPopup)
                    {
                        VictoryPopup.Show(VM.ElapsedTime, VM.MoveCount.Value, VM.LevelIndex < LevelDatabase.GetLevelCount(VM.PuzzleType) - 1);
                    }
                }));
            }
        }

        private System.Collections.IEnumerator TimerRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                if (VM != null && !VM.IsPuzzleSolved.Value)
                    VM.UpdateTimer(1f);
            }
        }

        // --- 按钮回调 ---

        protected virtual void OnBackClicked()
        {
            UIManager.Instance.Close(this);
        }

        protected virtual void OnUndoClicked()
        {
            VM?.Undo();
            RenderGrid();
        }

        protected virtual void OnHintClicked()
        {
            VM?.RequestHint();
            RenderGrid();
        }

        protected virtual void OnNextLevel()
        {
            int nextIndex = VM.LevelIndex + 1;
            if (nextIndex < LevelDatabase.GetLevelCount(VM.PuzzleType))
            {
                var nextData = LevelDatabase.CreateLevel(VM.PuzzleType, nextIndex);
                UIManager.Instance.Open(GetType().Name, nextData);
            }
        }

        protected virtual void OnBackToMenu()
        {
            UIManager.Instance.Close(this);
        }
    }
