using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 测试 ViewModel。演示 BindableProperty 在热更层的正常使用。
/// 属性变化自动通知 UI 刷新。
/// </summary>
public class TestViewModel : ObservableObject
{
    public BindableProperty<string> Title = new BindableProperty<string>("测试面板");
    public BindableProperty<int> ClickCount = new BindableProperty<int>(0);
    public BindableProperty<float> Progress = new BindableProperty<float>(0.5f);
    public BindableProperty<bool> IsToggleOn = new BindableProperty<bool>(false);
    public BindableProperty<string> InputText = new BindableProperty<string>("");

    public List<TestItemData> Items = new List<TestItemData>();

    public TestViewModel()
    {
        ForwardProperty(nameof(Title), Title);
        ForwardProperty(nameof(ClickCount), ClickCount);
        ForwardProperty(nameof(Progress), Progress);
        ForwardProperty(nameof(IsToggleOn), IsToggleOn);
        ForwardProperty(nameof(InputText), InputText);

        for (int i = 0; i < 20; i++)
            Items.Add(new TestItemData { Name = $"Item {i + 1}", Value = Random.Range(10, 100) });
    }

    public void OnButtonClicked()
    {
        ClickCount.Value++;
        Progress.Value = Mathf.Repeat(Progress.Value + 0.1f, 1f);
        Title.Value = $"点击了 {ClickCount.Value} 次";
    }

    public override void Dispose()
    {
        Title.Clear(); ClickCount.Clear(); Progress.Clear();
        IsToggleOn.Clear(); InputText.Clear();
        Items.Clear();
        base.Dispose();
    }
}

public class TestItemData
{
    public string Name;
    public int Value;
}
