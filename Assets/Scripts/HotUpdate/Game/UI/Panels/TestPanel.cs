using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 测试面板——放在 HotUpdate 程序集中，验证热更 DLL 加载和反射创建。
/// 功能：数据绑定、按钮事件、Slider/Toggle 绑定、列表展示。
/// </summary>
public class TestPanel : UIPanel
{
    [Header("文本")]
    [SerializeField] private TMP_Text _titleText;

    [Header("按钮")]
    [SerializeField] private Button _clickButton;
    [SerializeField] private Button _closeButton;

    [Header("进度和开关")]
    [SerializeField] private Slider _progressSlider;
    [SerializeField] private Toggle _testToggle;

    [Header("列表")]
    [SerializeField] private UIList _itemList;

    [Header("其他")]
    [SerializeField] private GameObject _emptyTip;

    private TestViewModel _vm;
    private readonly List<Action> _unbind = new List<Action>();

    protected override void OnOpen(object data)
    {
        Debug.Log($"[TestPanel] OnOpen, data={data}");

        _vm = new TestViewModel();

        _unbind.Add(_titleText.BindTo(_vm.Title));
        _unbind.Add(_clickButton.BindClick(_vm.OnButtonClicked));
        _unbind.Add(_closeButton.BindClick(OnCloseClicked));
        _unbind.Add(_progressSlider.BindTo(_vm.Progress));
        _unbind.Add(_testToggle.BindTo(_vm.IsToggleOn));
        _unbind.Add(_emptyTip.BindInactive(_vm.ClickCount));

        if (_itemList)
            _itemList.SetData(_vm.Items, BindListItem);

        _vm.PropertyChanged += OnVMChanged;
    }

    protected override void OnClose()
    {
        foreach (var u in _unbind) u.Invoke();
        _unbind.Clear();

        if (_vm != null)
        {
            _vm.PropertyChanged -= OnVMChanged;
            _vm.Dispose();
            _vm = null;
        }
    }

    private void OnCloseClicked() => UIManager.Instance.Close(this);

    private void BindListItem(GameObject go, object data, int index)
    {
        var item = data as TestItemData;
        if (item == null) return;

        var nameT = go.transform.Find("NameText");
        if (nameT) nameT.GetComponent<TMP_Text>().text = item.Name;

        var valT = go.transform.Find("ValueText");
        if (valT) valT.GetComponent<TMP_Text>().text = item.Value.ToString();
    }

    private void OnVMChanged(string prop)
    {
        Debug.Log($"[TestPanel] VM 属性变化: {prop}");
    }
}
