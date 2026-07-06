using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 稀有度徽章——根据稀有度显示不同颜色的边框。
/// 挂在物品图标外层的 Image 上。
/// </summary>
public class RarityBadgeWidget : MonoBehaviour
{
    [SerializeField] private Image _frame;

    private static readonly Color[] RarityColors = new[]
    {
        new Color(0.6f, 0.6f, 0.6f),  // Common 灰
        new Color(0.3f, 0.8f, 0.3f),  // Uncommon 绿
        new Color(0.3f, 0.5f, 1.0f),  // Rare 蓝
        new Color(0.7f, 0.3f, 1.0f),  // Epic 紫
        new Color(1.0f, 0.75f, 0.1f), // Legendary 金
    };

    public void SetRarity(ItemRarity rarity)
    {
        if (_frame == null) return;
        int idx = (int)rarity;
        if (idx < RarityColors.Length)
            _frame.color = RarityColors[idx];
    }
}
