using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Button 便捷扩展：防连点、延迟恢复、音效。
/// </summary>
public static class UIButtonExtensions
{
    /// <summary>冷却防连点</summary>
    public static void AddCooldown(this Button button, float seconds)
    {
        var cd = button.gameObject.GetComponent<ButtonCooldown>();
        if (!cd) cd = button.gameObject.AddComponent<ButtonCooldown>();
        cd.Init(button, seconds);
    }

    /// <summary>延迟恢复 interactable</summary>
    public static void SetInteractableDelayed(this Button button, float delay)
    {
        button.interactable = false;
        button.StartCoroutine(DelayedRoutine(button, delay));
    }

    private static IEnumerator DelayedRoutine(Button btn, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (btn) btn.interactable = true;
    }

    /// <summary>点击时播放音效（需 AudioSource 组件）</summary>
    public static void AddClickSound(this Button button, AudioClip clip)
    {
        button.onClick.AddListener(() =>
        {
            var src = button.GetComponent<AudioSource>();
            if (!src) src = button.gameObject.AddComponent<AudioSource>();
            src.PlayOneShot(clip);
        });
    }
}

/// <summary>按钮冷却组件（内部使用）</summary>
internal class ButtonCooldown : MonoBehaviour
{
    private Button _btn;
    private float _cd;
    private bool _cooling;

    public void Init(Button btn, float sec)
    {
        _btn = btn; _cd = sec;
        btn.onClick.AddListener(OnClick);
    }

    private void OnClick() { if (!_cooling) StartCoroutine(Routine()); }

    private IEnumerator Routine()
    {
        _cooling = true; _btn.interactable = false;
        yield return new WaitForSeconds(_cd);
        _btn.interactable = true; _cooling = false;
    }
}
