using UnityEngine;

/// <summary>
/// 轻量级 UI 子组件基类。用于面板内的可复用小部件（血条、货币、Buff 图标等）。
/// 自身不参与 UI 栈管理，由父 Panel 负责创建/回收。
/// 热更层的 Widget 继承此类。
/// </summary>
public abstract class UIWidget : MonoBehaviour
{
    public virtual void Init(object data) { }
    public virtual void Dispose() { }

    protected T GetWidgetComponent<T>(string path) where T : Component
    {
        var child = transform.Find(path);
        if (!child) { Debug.LogWarning($"[UIWidget] 找不到: {path}"); return null; }
        return child.GetComponent<T>();
    }
}
