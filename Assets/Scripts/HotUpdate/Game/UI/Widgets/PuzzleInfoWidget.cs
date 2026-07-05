using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

    /// <summary>
    /// 游戏面板顶部信息栏。显示关卡名称、计时器、撤销/提示按钮。
    /// 挂在 PuzzleInfoWidget.prefab 上，作为 PuzzleGamePanel 的子控件。
    /// </summary>
    public class PuzzleInfoWidget : MonoBehaviour
    {
        [SerializeField] private TMP_Text _levelNameText;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _hintButton;

        public event Action OnBackClicked;
        public event Action OnUndoClicked;
        public event Action OnHintClicked;

        private void Awake()
        {
            if (_backButton) _backButton.onClick.AddListener(() => OnBackClicked?.Invoke());
            if (_undoButton) _undoButton.onClick.AddListener(() => OnUndoClicked?.Invoke());
            if (_hintButton) _hintButton.onClick.AddListener(() => OnHintClicked?.Invoke());
        }

        public void SetLevelName(string name)
        {
            if (_levelNameText) _levelNameText.text = name;
        }

        public void SetTimer(string time)
        {
            if (_timerText) _timerText.text = time;
        }

        public void SetUndoEnabled(bool enabled)
        {
            if (_undoButton) _undoButton.interactable = enabled;
        }
    }
