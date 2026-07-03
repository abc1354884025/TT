/// <summary>
/// UI 层级枚举。值越大 SortingOrder 越高，显示越靠前。
/// </summary>
public enum UILayer
{
    Background = 0,  // 背景层
    Normal     = 100, // 普通层：主菜单、背包、商城
    Popup      = 200, // 弹窗层：确认框、提示框
    Top        = 300, // 顶层：Loading、转场遮罩
    System     = 400  // 系统层：GM 工具、FPS 显示
}
