#if LOCAL_PLAYTEST
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CorpPresence.UI;

// The isolated Unity host owns the input slot. Tuples use only shared runtime types,
// so neither mod needs to load or resolve the other's assembly.
internal static class PanelPlaytest
{
    private const string InputSlot = "CorpLocalPlaytest.Pointer.v1";
    private const string ControlsSlot = "CorpLocalPlaytest.PanelControls.v1";
    private static readonly bool Enabled = Array.IndexOf(Environment.GetCommandLineArgs(), "-corp-playtest") >= 0;
    private static readonly List<string> Controls = new();
    private static Rect window;
    private static float panelScale;
    private static bool collecting;

    internal static Tuple<int, Vector2, bool, bool, bool, float> Frame
    {
        get
        {
            if (!Enabled) return null;
            var sample = AppDomain.CurrentDomain.GetData(InputSlot) as Tuple<int, Vector2, bool, bool, bool, float>;
            return sample != null && sample.Item1 == Time.frameCount ? sample : null;
        }
    }

    internal static void Begin(Rect panel, float scale)
    {
        collecting = Enabled && !CargoSortWindow.Active;
        Controls.Clear();
        window = panel;
        panelScale = scale;
    }

    [Serializable]
    private sealed class Control
    {
        public string name, label;
        public int x, y;
        public bool interactable;
    }

    internal static void Record(Rect hit, Rect? clip, string text, bool enabled)
    {
        if (!collecting || Controls.Count >= 64) return;
        if (clip.HasValue) hit = Intersect(hit, clip.Value);
        hit = Intersect(hit, new Rect(Vector2.zero, window.size));
        var pixels = new Rect((hit.position + window.position) * panelScale, hit.size * panelScale);
        pixels = Intersect(pixels, new Rect(0, 0, Screen.width, Screen.height));
        if (pixels.width <= 0 || pixels.height <= 0) return;
        int x = Mathf.FloorToInt(pixels.center.x), y = Mathf.FloorToInt(pixels.center.y);
        if (!pixels.Contains(new Vector2(x + .5f, y + .5f))) return;
        string label = string.IsNullOrEmpty(text) ? "" : text.Substring(0, Math.Min(text.Length, 80));
        Controls.Add(JsonUtility.ToJson(new Control { name = label, label = label, x = x, y = y, interactable = enabled }));
    }

    private static Rect Intersect(Rect a, Rect b)
    {
        float x = Mathf.Max(a.xMin, b.xMin), y = Mathf.Max(a.yMin, b.yMin);
        return new Rect(x, y, Mathf.Max(0, Mathf.Min(a.xMax, b.xMax) - x), Mathf.Max(0, Mathf.Min(a.yMax, b.yMax) - y));
    }

    internal static void Publish()
    {
        if (!Enabled) return;
        AppDomain.CurrentDomain.SetData(ControlsSlot, collecting ? "[" + string.Join(",", Controls) + "]" : null);
        collecting = false;
    }

    internal static void ClearControls()
    {
        collecting = false;
        Controls.Clear();
        if (Enabled) AppDomain.CurrentDomain.SetData(ControlsSlot, null);
    }
}
#endif
