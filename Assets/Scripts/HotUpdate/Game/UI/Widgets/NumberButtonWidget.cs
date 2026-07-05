using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

    /// <summary>
    /// 数独数字键盘按钮（1-9 和擦除）。
    /// 挂在 NumberButtonWidget.prefab 上。
    /// </summary>
    public class NumberButtonWidget : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;

        public int NumberValue { get; private set; }
        public event Action<int> OnNumberClicked;

        private void Awake()
        {
            if (_button)
                _button.onClick.AddListener(OnClick);
        }

        public void Init(int number)
        {
            NumberValue = number;
            if (_label)
                _label.text = number > 0 ? number.ToString() : "×";  // 0 = erase
        }

        private void OnClick()
        {
            OnNumberClicked?.Invoke(NumberValue);
        }

        /// <summary>设置剩余可用数量（数独中某个数字还能填几次）</summary>
        public void SetRemainingCount(int count)
        {
            if (_label && NumberValue > 0)
                _label.text = count > 0 ? NumberValue.ToString() : "-";
        }
    }
