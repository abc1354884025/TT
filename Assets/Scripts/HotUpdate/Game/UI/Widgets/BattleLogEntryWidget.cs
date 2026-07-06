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
        if (_logText == null) return;
        _logText.text = text;
        _logText.color = isCrit
            ? Color.yellow
            : isPlayerAttack ? new Color(0.5f, 0.8f, 1f) : new Color(1f, 0.5f, 0.5f);
    }
}
