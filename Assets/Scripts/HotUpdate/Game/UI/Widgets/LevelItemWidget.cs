using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

    /// <summary>
    /// 关卡选择列表项控件。用于 LevelSelectPanel 的 UIList。
    /// 挂在 LevelItemWidget.prefab 上。
    /// </summary>
    public class LevelItemWidget : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _levelLabel;
        [SerializeField] private Image _lockIcon;
        [SerializeField] private Image _starIcon;     // 通关标记
        [SerializeField] private TMP_Text _bestTimeText;

        private LevelSelectItemData _data;
        public event Action<LevelSelectItemData> OnClicked;

        private void Awake()
        {
            if (_button)
                _button.onClick.AddListener(() => OnClicked?.Invoke(_data));
        }

        public void Bind(LevelSelectItemData data)
        {
            _data = data;

            if (_levelLabel)
                _levelLabel.text = data.Label;

            if (_button)
                _button.interactable = data.IsUnlocked;

            if (_lockIcon)
                _lockIcon.gameObject.SetActive(!data.IsUnlocked);

            if (_starIcon)
                _starIcon.gameObject.SetActive(data.IsCompleted);

            if (_bestTimeText)
            {
                if (data.IsCompleted && data.BestTime > 0)
                {
                    int minutes = (int)(data.BestTime / 60);
                    int seconds = (int)(data.BestTime % 60);
                    _bestTimeText.text = $"{minutes:D2}:{seconds:D2}";
                    _bestTimeText.gameObject.SetActive(true);
                }
                else
                {
                    _bestTimeText.gameObject.SetActive(false);
                }
            }
        }
    }
