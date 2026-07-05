using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

    /// <summary>
    /// 主菜单面板。显示四种益智游戏的入口。
    /// </summary>
    public class MainMenuPanel : UIPanel
    {
        [Header("标题")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _progressText;

        [Header("游戏入口按钮")]
        [SerializeField] private Button _numLinkButton;    // 数回
        [SerializeField] private Button _sudokuButton;     // 数独
        [SerializeField] private Button _nurikabeButton;   // 数墙
        [SerializeField] private Button _hashiButton;      // 数桥

        [Header("其他")]
        [SerializeField] private Button _settingsButton;

        private MainMenuViewModel _vm;
        private readonly List<Action> _unbind = new List<Action>();

        protected override void OnOpen(object data)
        {
            Debug.Log($"[MainMenuPanel] OnOpen 被调用, GameObject 名称: {gameObject.name}, 激活: {gameObject.activeSelf}");

            _vm = new MainMenuViewModel();

            // 诊断：检查所有引用是否已赋值
            Debug.Log($"[MainMenuPanel] 引用检查:" +
                $"\n  _titleText = {(_titleText ? "✓" : "✗ 未赋值")}" +
                $"\n  _progressText = {(_progressText ? "✓" : "✗ 未赋值")}" +
                $"\n  _numLinkButton = {(_numLinkButton ? "✓" : "✗ 未赋值")}" +
                $"\n  _sudokuButton = {(_sudokuButton ? "✓" : "✗ 未赋值")}" +
                $"\n  _nurikabeButton = {(_nurikabeButton ? "✓" : "✗ 未赋值")}" +
                $"\n  _hashiButton = {(_hashiButton ? "✓" : "✗ 未赋值")}" +
                $"\n  _settingsButton = {(_settingsButton ? "✓" : "✗ 未赋值")}");

            // 标题和进度
            if (_titleText)
                _titleText.text = "益智游戏合集";

            if (_progressText)
                _unbind.Add(_progressText.BindTo(_vm.ProgressSummary));

            // 四个游戏按钮
            if (_numLinkButton) _unbind.Add(_numLinkButton.BindClick(() => OpenLevelSelect(PuzzleType.NumberLink)));
            if (_sudokuButton) _unbind.Add(_sudokuButton.BindClick(() => OpenLevelSelect(PuzzleType.Sudoku)));
            if (_nurikabeButton) _unbind.Add(_nurikabeButton.BindClick(() => OpenLevelSelect(PuzzleType.Nurikabe)));
            if (_hashiButton) _unbind.Add(_hashiButton.BindClick(() => OpenLevelSelect(PuzzleType.HashiBridge)));

            // 设置按钮
            if (_settingsButton) _unbind.Add(_settingsButton.BindClick(OpenSettings));

            Debug.Log($"[MainMenuPanel] 已绑定 {_unbind.Count} 个事件");
        }

        protected override void OnShow()
        {
            // 每次显示时刷新进度
            _vm?.RefreshProgress();
        }

        protected override void OnClose()
        {
            foreach (var u in _unbind) u.Invoke();
            _unbind.Clear();
            _vm?.Dispose();
            _vm = null;
        }

        private void OpenLevelSelect(PuzzleType type)
        {
            Debug.Log($"[MainMenu] 点击了 {type}");  // 加这行
            _vm.SelectPuzzle(type);
            UIManager.Instance.Open("LevelSelectPanel", type);
        }

        private void OpenSettings()
        {
            UIManager.Instance.Open("SettingsPanel");
        }
    }
