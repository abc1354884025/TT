using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗日志行——彩色文字显示。
/// </summary>
public class BattleLogEntryWidget : MonoBehaviour
{
    [SerializeField] private Text _logText;

    public void SetEntry(string text, bool isPlayerAttack, bool isCrit)
    {
        EnsureText();
        _logText.text = text;
        _logText.color = isCrit
            ? new Color(1f, 0.78f, 0.28f)
            : isPlayerAttack ? new Color(0.35f, 0.82f, 0.96f) : new Color(0.96f, 0.42f, 0.46f);
    }

    private void EnsureText()
    {
        if (_logText) return;

        var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(transform, false);
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(16f, 4f);
        rect.offsetMax = new Vector2(-16f, -4f);

        _logText = textObject.GetComponent<Text>();
        _logText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _logText.fontSize = 27;
        _logText.alignment = TextAnchor.MiddleLeft;
        _logText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _logText.verticalOverflow = VerticalWrapMode.Truncate;
        _logText.raycastTarget = false;
    }
}
