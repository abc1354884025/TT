using UnityEngine;

/// <summary>
/// 主菜单 ViewModel。
/// </summary>
public class MainMenuViewModel : ObservableObject
{
    public BindableProperty<string> Title = new BindableProperty<string>("新游戏");
    public BindableProperty<string> Subtitle = new BindableProperty<string>("新玩法开发中");

    public override void Dispose()
    {
        Title.Clear(); Subtitle.Clear();
        base.Dispose();
    }
}
