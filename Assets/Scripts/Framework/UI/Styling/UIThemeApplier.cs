using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies the shared vertical-game visual language to the three core panels.
/// The component intentionally uses names already required by the panel scripts,
/// so it does not add HotUpdate references to the AOT UI framework.
/// </summary>
public sealed class UIThemeApplier : MonoBehaviour
{
    private static readonly Color Background = Hex("07111D");
    private static readonly Color Surface = Hex("102238");
    private static readonly Color SurfaceRaised = Hex("18324B");
    private static readonly Color Primary = Hex("24B8D8");
    private static readonly Color Gold = Hex("F4C65B");
    private static readonly Color Danger = Hex("E95D68");
    private static readonly Color TextPrimary = Hex("F2F7FC");
    private static readonly Color TextMuted = Hex("91A9BE");

    private bool _applied;

    private void Awake()
    {
        Apply();
    }

    private void Apply()
    {
        if (_applied) return;
        _applied = true;

        var background = GetComponent<Image>();
        if (background) background.color = Background;

        switch (name)
        {
            case "MainMenuPanel":
                ApplyMainMenu();
                break;
            case "PreparePanel":
                ApplyPrepare();
                break;
            case "BattlePanel":
                ApplyBattle();
                break;
        }
    }

    private void ApplyMainMenu()
    {
        CreateBlock("Theme_TopGlow", new Vector2(0f, 690f), new Vector2(940f, 620f), WithAlpha(Primary, 0.07f));
        CreateBlock("Theme_MenuCard", new Vector2(0f, -20f), new Vector2(600f, 500f), WithAlpha(Surface, 0.96f), true);

        SetRect("title", new Vector2(0f, 410f), new Vector2(900f, 150f));
        StyleText("title", TextPrimary, 76, "背包乱斗");

        SetRect("progress", new Vector2(0f, 285f), new Vector2(760f, 64f));
        StyleText("progress", TextMuted, 28, "整理背包 · 组合装备 · 自动战斗");

        SetRect("_startButton", new Vector2(0f, 45f), new Vector2(430f, 96f));
        StyleButton("_startButton", Primary, "开始游戏", 36);

        SetRect("_quitButton", new Vector2(0f, -100f), new Vector2(430f, 88f));
        StyleButton("_quitButton", SurfaceRaised, "退出游戏", 32);

        var hero = Find("Image");
        if (hero)
        {
            SetRect(hero as RectTransform, new Vector2(0f, 740f), new Vector2(620f, 620f));
            var image = hero.GetComponent<Image>();
            if (image) image.color = WithAlpha(Gold, 0.1f);
        }
    }

    private void ApplyPrepare()
    {
        CreateBlock("Theme_Header", new Vector2(0f, 835f), new Vector2(1010f, 170f), WithAlpha(Surface, 0.98f), true);
        CreateBlock("Theme_StatsCard", new Vector2(0f, 665f), new Vector2(1010f, 150f), WithAlpha(Surface, 0.88f), true);
        CreateBlock("Theme_BagCard", new Vector2(-255f, 80f), new Vector2(530f, 930f), WithAlpha(Surface, 0.78f), true);
        CreateBlock("Theme_ShopCard", new Vector2(305f, 80f), new Vector2(500f, 930f), WithAlpha(Surface, 0.78f), true);
        CreateBlock("Theme_InventoryCard", new Vector2(0f, -675f), new Vector2(1010f, 245f), WithAlpha(Surface, 0.9f), true);

        SetRect("_backButton", new Vector2(-430f, 835f), new Vector2(140f, 72f));
        StyleButton("_backButton", SurfaceRaised, "返回", 27);

        SetRect("_roundText", new Vector2(-95f, 835f), new Vector2(270f, 70f));
        StyleText("_roundText", TextPrimary, 34, "第 1 回合");

        SetRect("_goldText", new Vector2(270f, 835f), new Vector2(300f, 70f));
        StyleText("_goldText", Gold, 36, "金币 0");

        StyleStat("_atkBar", new Vector2(-330f, 665f), Danger);
        StyleStat("_defBar", new Vector2(0f, 665f), Primary);
        StyleStat("_hpBar", new Vector2(330f, 665f), Hex("55D68B"));

        SetRect("BackpackGridWidget", new Vector2(-255f, 55f), new Vector2(500f, 820f));
        SetRect("_shopList", new Vector2(305f, 50f), new Vector2(450f, 790f));
        SetRect("_inventoryList", new Vector2(-80f, -675f), new Vector2(800f, 190f));

        SetRect("_refreshShopButton", new Vector2(305f, 540f), new Vector2(310f, 78f));
        StyleButton("_refreshShopButton", SurfaceRaised, "刷新商店", 28);

        SetRect("_battleButton", new Vector2(370f, -840f), new Vector2(310f, 96f));
        StyleButton("_battleButton", Gold, "开始战斗", 34, Background);

        TintRootGraphic("BackpackGridWidget", WithAlpha(SurfaceRaised, 0.55f));
        TintRootGraphic("_shopList", WithAlpha(SurfaceRaised, 0.45f));
        TintRootGraphic("_inventoryList", WithAlpha(SurfaceRaised, 0.45f));
    }

    private void ApplyBattle()
    {
        CreateBlock("Theme_CombatHeader", new Vector2(0f, 760f), new Vector2(1010f, 380f), WithAlpha(Surface, 0.95f), true);
        CreateBlock("Theme_LogCard", new Vector2(0f, -135f), new Vector2(1010f, 1220f), WithAlpha(Surface, 0.78f), true);

        SetRect("_playerNameText", new Vector2(-270f, 835f), new Vector2(420f, 62f));
        StyleText("_playerNameText", Primary, 39, "玩家");
        SetRect("_playerHPText", new Vector2(-270f, 770f), new Vector2(420f, 50f));
        StyleText("_playerHPText", TextPrimary, 28, "0/0");
        SetRect("_playerHPBar", new Vector2(-270f, 715f), new Vector2(420f, 38f));
        StyleSlider("_playerHPBar", Primary);

        SetRect("_enemyNameText", new Vector2(270f, 835f), new Vector2(420f, 62f));
        StyleText("_enemyNameText", Danger, 39, "敌人");
        SetRect("_enemyHPText", new Vector2(270f, 770f), new Vector2(420f, 50f));
        StyleText("_enemyHPText", TextPrimary, 28, "0/0");
        SetRect("_enemyHPBar", new Vector2(270f, 715f), new Vector2(420f, 38f));
        StyleSlider("_enemyHPBar", Danger);

        SetRect("_resultText", new Vector2(0f, 540f), new Vector2(820f, 100f));
        StyleText("_resultText", Gold, 48, "战斗进行中");

        SetRect("_continueButton", new Vector2(0f, -835f), new Vector2(380f, 96f));
        StyleButton("_continueButton", Gold, "领取奖励", 34, Background);
    }

    private void StyleStat(string objectName, Vector2 position, Color accent)
    {
        var transform = Find(objectName);
        if (!transform) return;

        SetRect(transform as RectTransform, position, new Vector2(300f, 116f));
        var graphics = transform.GetComponentsInChildren<Graphic>(true);
        foreach (var graphic in graphics)
        {
            if (graphic.gameObject.name == "_icon") graphic.color = accent;
            else if (graphic is Text text) text.color = text.gameObject.name == "_valueText" ? TextPrimary : TextMuted;
        }
    }

    private void StyleSlider(string objectName, Color fillColor)
    {
        var transform = Find(objectName);
        if (!transform) return;

        var graphics = transform.GetComponentsInChildren<Image>(true);
        foreach (var image in graphics)
        {
            if (image.gameObject.name == "Fill") image.color = fillColor;
            else if (image.gameObject.name == "Background") image.color = WithAlpha(TextMuted, 0.22f);
            else if (image.gameObject.name == "Handle") image.color = Color.clear;
        }
    }

    private void StyleButton(string objectName, Color color, string label, int fontSize, Color? textColor = null)
    {
        var transform = Find(objectName);
        if (!transform) return;

        var image = transform.GetComponent<Image>();
        if (image) color = WithAlpha(color, 1f);
        if (image) image.color = color;

        var button = transform.GetComponent<Button>();
        if (button)
        {
            var colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.12f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = WithAlpha(color, 0.35f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
        }

        foreach (var graphic in transform.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic.transform == transform) continue;
            graphic.color = textColor ?? TextPrimary;
            SetTextReflection(graphic, label, fontSize);
        }
    }

    private void StyleText(string objectName, Color color, int fontSize, string fallbackText)
    {
        var transform = Find(objectName);
        if (!transform) return;

        foreach (var graphic in transform.GetComponents<Graphic>())
        {
            graphic.color = color;
            SetTextReflection(graphic, fallbackText, fontSize);
        }
    }

    private static void SetTextReflection(Graphic graphic, string fallbackText, int fontSize)
    {
        if (graphic is Text text)
        {
            if (string.IsNullOrWhiteSpace(text.text) || text.text == "New Text" || text.text == "Button")
                text.text = fallbackText;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(18, fontSize - 10);
            text.resizeTextMaxSize = fontSize;
            return;
        }

        var type = graphic.GetType();
        SetPropertyIfPresent(type, graphic, "text", current =>
            string.IsNullOrWhiteSpace(current as string) || (string)current == "New Text" || (string)current == "Button"
                ? fallbackText
                : current);
        SetPropertyIfPresent(type, graphic, "fontSize", _ => (float)fontSize);
        SetPropertyIfPresent(type, graphic, "fontStyle", current =>
        {
            var enumType = current.GetType();
            return Enum.IsDefined(enumType, "Bold") ? Enum.Parse(enumType, "Bold") : current;
        });
    }

    private static void SetPropertyIfPresent(Type type, object target, string propertyName, Func<object, object> valueFactory)
    {
        var property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property == null || !property.CanRead || !property.CanWrite) return;
        try
        {
            var current = property.GetValue(target, null);
            property.SetValue(target, valueFactory(current), null);
        }
        catch (Exception)
        {
            // TextMeshPro is optional for UIFramework; unsupported versions keep prefab values.
        }
    }

    private void TintRootGraphic(string objectName, Color color)
    {
        var transform = Find(objectName);
        if (!transform) return;
        var image = transform.GetComponent<Image>();
        if (image) image.color = color;
    }

    private RectTransform CreateBlock(string objectName, Vector2 position, Vector2 size, Color color, bool outline = false)
    {
        var existing = transform.Find(objectName) as RectTransform;
        if (existing) return existing;

        var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = gameObject.layer;
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        SetRect(rect, position, size);
        go.GetComponent<Image>().color = color;
        if (outline)
        {
            var border = go.AddComponent<Outline>();
            border.effectColor = WithAlpha(Primary, 0.2f);
            border.effectDistance = new Vector2(1f, -1f);
        }
        rect.SetAsFirstSibling();
        return rect;
    }

    private Transform Find(string objectName)
    {
        var transforms = GetComponentsInChildren<Transform>(true);
        foreach (var child in transforms)
            if (child.name == objectName) return child;
        return null;
    }

    private void SetRect(string objectName, Vector2 position, Vector2 size)
    {
        SetRect(Find(objectName) as RectTransform, position, size);
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (!rect) return;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static Color Hex(string value)
    {
        return ColorUtility.TryParseHtmlString("#" + value, out var color) ? color : Color.white;
    }
}
