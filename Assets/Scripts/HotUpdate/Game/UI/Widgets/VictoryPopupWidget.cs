using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

    /// <summary>
    /// 通关弹窗控件。作为游戏面板的子控件，激活时显示。
    /// 挂在 VictoryPopupWidget.prefab 上。
    /// </summary>
    public class VictoryPopupWidget : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _titleText;        // "恭喜通关！"
        [SerializeField] private TMP_Text _statsText;        // "用时 02:35  |  步数 42"
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Button _backToMenuButton;

        public event Action OnNextLevel;
        public event Action OnBackToMenu;

        private void Awake()
        {
            if (_nextLevelButton) _nextLevelButton.onClick.AddListener(() => OnNextLevel?.Invoke());
            if (_backToMenuButton) _backToMenuButton.onClick.AddListener(() => OnBackToMenu?.Invoke());
            Hide();
        }

        public void Show(float elapsedTime, int moveCount, bool hasNextLevel)
        {
            if (_root) _root.SetActive(true);

            if (_statsText)
            {
                int min = (int)(elapsedTime / 60);
                int sec = (int)(elapsedTime % 60);
                _statsText.text = $"用时 {min:D2}:{sec:D2}  |  步数 {moveCount}";
            }

            if (_nextLevelButton)
                _nextLevelButton.gameObject.SetActive(hasNextLevel);
        }

        public void Hide()
        {
            if (_root) _root.SetActive(false);
        }
    }
