using UnityEngine;

/// <summary>
/// 主菜单 ViewModel。
/// </summary>
public class MainMenuViewModel : ObservableObject
{
    public BindableProperty<string> Title = new BindableProperty<string>("背包乱斗");
    public BindableProperty<string> Subtitle = new BindableProperty<string>("Backpack Brawl");

    public override void Dispose()
    {
        Title.Clear(); Subtitle.Clear();
        base.Dispose();
    }
}
