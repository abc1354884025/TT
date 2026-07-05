using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

    /// <summary>
    /// 设置面板。显示在 Popup 层，覆盖主菜单。
    /// </summary>
    public class SettingsPanel : UIPanel
    {
        [Header("标题")]
        [SerializeField] private TMP_Text _titleText;

        [Header("开关")]
        [SerializeField] private Toggle _soundToggle;
        [SerializeField] private Toggle _vibrationToggle;

        [Header("按钮")]
        [SerializeField] private Button _resetProgressButton;
        [SerializeField] private Button _closeButton;

        private SettingsViewModel _vm;
        private readonly List<Action> _unbind = new List<Action>();

        protected override void OnOpen(object data)
        {
            _vm = new SettingsViewModel();

            if (_titleText)
                _titleText.text = "设置";

            if (_soundToggle)
                _soundToggle.BindTwoWay(_vm.SoundEnabled);

            if (_vibrationToggle)
                _vibrationToggle.BindTwoWay(_vm.VibrationEnabled);

            if (_resetProgressButton)
                _unbind.Add(_resetProgressButton.BindClick(OnResetProgress));

            if (_closeButton)
                _unbind.Add(_closeButton.BindClick(OnCloseClicked));
        }

        protected override void OnClose()
        {
            foreach (var u in _unbind) u.Invoke();
            _unbind.Clear();
            _vm?.Dispose();
            _vm = null;
        }

        private void OnResetProgress()
        {
            _vm.ResetAllProgress();
            Debug.Log("[SettingsPanel] 进度已重置");
        }

        private void OnCloseClicked()
        {
            UIManager.Instance.Close(this);
        }
    }
