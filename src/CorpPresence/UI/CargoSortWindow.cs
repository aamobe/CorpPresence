using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CorpPresence.UI;

internal static class CargoSortWindow
{
    private static readonly string[] Featured = { "NARCOTIC", CargoWarehouse.TobaccoAndAlcohol, "LUXURY_ITEM" };
    private static Action<string[]> _save;
    private static Action _rename;
    private static HashSet<string> _checked;
    private static string[] _options;
    private static string[] _labels;
    private static int _page;
    private static Vector2 _scroll;
    private static Theme _theme;
    private static GUIStyle _toggle;
    private static GUIStyle _title;
    private static GUIStyle _hint;
    private static GUIStyle _button;
    private static GUIStyle _primaryButton;
    private static GUIStyle _frame;
    private static Texture2D _closeIcon;
    private static bool _nativeFrame;
    private static readonly Color Surface = new Color32(136, 148, 138, 255);
    private static readonly Color Header = new Color32(178, 107, 78, 255);
    private static readonly Color Ink = new Color32(23, 31, 27, 255);
    private static readonly Color Selected = new Color32(8, 56, 47, 255);
    private static readonly Color ButtonText = new Color32(255, 255, 255, 255);

    internal static bool Active => _save != null;

    internal static void Open(int page, string[] filters, Action<string[]> save, Action rename)
    {
        _page = page;
        _checked = new HashSet<string>(filters, StringComparer.Ordinal);
        _options = Featured.Concat(TypeHelper.ALL_TYPES_LIST).Concat(filters).Distinct(StringComparer.Ordinal).ToArray();
        _labels = _options.Select(Label).ToArray();
        _scroll = Vector2.zero;
        _save = save;
        _rename = rename;
    }

    internal static void Close()
    {
        _save = null;
        _rename = null;
        _checked = null;
        _options = null;
        _labels = null;
        _theme = null;
        _toggle = null;
        _title = null;
        _hint = null;
        _button = null;
        _primaryButton = null;
        _frame = null;
        _closeIcon = null;
    }

    internal static void Draw()
    {
        if (!Active) return;
        if (_theme == null)
        {
            _theme = Theme.Spacer();
            _toggle = Style(GUI.skin.toggle, 19, Ink, Selected);
            _title = Style(_theme.Title, 22, Ink, Ink);
            _hint = Style(_theme.Small, 16, Ink, Ink);
            SetNativeChrome();
        }
        float width = Mathf.Min(610f, Screen.width - 24f);
        float height = Mathf.Min(470f, Screen.height - 24f);
        var rect = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
        int depth = GUI.depth;
        GUI.depth = -10;
        try { GUI.ModalWindow(0x435053, rect, DrawWindow, GUIContent.none, _frame); }
        finally { GUI.depth = depth; }
    }

    private static GUIStyle Style(GUIStyle template, int size, Color text, Color selected)
    {
        var style = new GUIStyle(template) { font = _theme.Body.font, fontSize = size };
        style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = text;
        style.onNormal.textColor = style.onHover.textColor = style.onActive.textColor = style.onFocused.textColor = selected;
        return style;
    }

    private static void SetNativeChrome()
    {
        var labeler = LabelerBlueUIManager.Instance;
        var panel = labeler != null ? labeler.panel : null;
        Texture2D nativeBackground = panel != null ? Tex.FromSprite(panel.GetComponent<Image>()?.sprite) : null;
        _nativeFrame = nativeBackground != null;
        _frame = new GUIStyle(_theme.Box);
        _frame.normal.background = _frame.onNormal.background = nativeBackground ?? Tex.Solid(Surface);
        _frame.border = _nativeFrame ? new RectOffset(8, 8, 38, 8) : new RectOffset();
        var inputImage = labeler != null && labeler.inputField != null ? labeler.inputField.targetGraphic as Image : null;
        Texture2D inputBackground = Tex.FromSprite(inputImage?.sprite);
        _button = Style(GUI.skin.button, 17, ButtonText, ButtonText);
        _button.normal.background = inputBackground ?? Tex.Solid(new Color32(100, 83, 85, 255));
        _button.hover.background = _button.active.background = _button.focused.background = Tex.Solid(new Color32(100, 83, 85, 255));
        _button.border = new RectOffset(2, 2, 2, 2);
        _primaryButton = new GUIStyle(_button) { fontStyle = FontStyle.Bold };
        var close = panel != null ? panel.transform.Find("CloseButton") : null;
        _closeIcon = close != null ? Tex.FromSprite(close.GetComponent<Image>()?.sprite) : null;
    }

    private static string Label(string code) => code switch
    {
        "NARCOTIC" => L.K("production.cargoWarehouse.controller.narcotics", "麻醉品", "Narcotics"),
        CargoWarehouse.TobaccoAndAlcohol => L.K("production.cargoWarehouse.controller.tobaccoAlcohol", "烟酒", "Tobacco & alcohol"),
        "LUXURY_ITEM" => L.K("production.cargoWarehouse.controller.luxury", "奢侈品", "Luxury goods"),
        _ => TypeHelper.ALL_TYPES_LIST.Contains(code) ? TypeHelper.GetTypeDisplayName(code) : code
    };

    private static void DrawWindow(int windowId)
    {
        float width = Mathf.Min(610f, Screen.width - 24f);
        float height = Mathf.Min(470f, Screen.height - 24f);
        if (!_nativeFrame) GUI.DrawTexture(new Rect(0, 0, width, 35f), Tex.Solid(Header));
        GUI.Label(new Rect(16f, 2f, width - 54f, 30f),
            L.F("production.cargoWarehouse.controller.page", "第 {0} 页 · 允许存放的类别", "Tab {0} · Allowed categories", _page + 1), _title);
        if (_closeIcon != null && GUI.Button(new Rect(width - 32f, 7f, 22f, 22f), _closeIcon, GUIStyle.none))
        {
            Close();
            return;
        }
        GUI.Label(new Rect(18f, 44f, width - 36f, 25f),
            L.K("production.cargoWarehouse.controller.unrestricted", "全部不勾选时接受通用物品。", "Leave all unchecked for unrestricted storage."), _hint);
        float column = (width - 54f) / 2f;
        var list = new Rect(16f, 77f, width - 32f, height - 137f);
        _scroll = GUI.BeginScrollView(list, _scroll,
            new Rect(0, 0, width - 54f, ((_options.Length + 1) / 2) * 33f));
        try
        {
            for (int i = 0; i < _options.Length; i++)
            {
                string code = _options[i];
                bool wasChecked = _checked.Contains(code);
                bool isChecked = GUI.Toggle(new Rect(8f + i % 2 * column, i / 2 * 33f, column - 14f, 31f), wasChecked, _labels[i], _toggle);
                if (isChecked && !wasChecked) _checked.Add(code);
                if (!isChecked && wasChecked) _checked.Remove(code);
            }
        }
        finally { GUI.EndScrollView(); }
        float buttonWidth = (width - 52f) / 3f;
        float footer = height - 46f;
        if (GUI.Button(new Rect(16f, footer, buttonWidth, 32f),
            L.K("production.cargoWarehouse.controller.cancel", "取消", "Cancel"), _button)) Close();
        else if (GUI.Button(new Rect(26f + buttonWidth, footer, buttonWidth, 32f),
            L.K("production.cargoWarehouse.controller.saveRename", "保存并改名", "Save and rename"), _button)) SaveAndClose(rename: true);
        else if (GUI.Button(new Rect(width - 16f - buttonWidth, footer, buttonWidth, 32f),
            L.K("production.cargoWarehouse.controller.save", "保存", "Save"), _primaryButton)) SaveAndClose(rename: false);
    }

    private static void SaveAndClose(bool rename)
    {
        Action<string[]> save = _save;
        Action renameTab = _rename;
        string[] filters = _options.Where(_checked.Contains).ToArray();
        Close();
        save(filters);
        if (rename) renameTab();
    }
}
