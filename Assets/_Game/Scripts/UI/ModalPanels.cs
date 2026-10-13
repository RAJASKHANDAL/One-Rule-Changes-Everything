using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Settings modal: master volume, SFX volume, screen shake toggle (all persisted).</summary>
public class SettingsPanel : MonoBehaviour
{
    public static SettingsPanel Create(Transform parent)
    {
        var rt = UIKit.Rect("SettingsPanel", parent);
        UIKit.Stretch(rt);
        var panel = rt.gameObject.AddComponent<SettingsPanel>();
        panel.Build(rt);
        rt.gameObject.SetActive(false);
        return panel;
    }

    public bool IsOpen => gameObject.activeSelf;
    public void Open() => gameObject.SetActive(true);
    public void Close() { GameSettings.Save(); gameObject.SetActive(false); }

    void Build(RectTransform root)
    {
        var dim = UIKit.Img(root, "Dim", UIKit.White, new Color(0.01f, 0.02f, 0.08f, 0.72f), true);
        UIKit.Stretch(dim.rectTransform);

        var inner = UIKit.Panel(root, "Window", UIKit.NavyDeep, UIKit.Border, out var outer, 22f, 3f, true);
        UIKit.Place(outer.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 650f));

        var title = UIKit.Label(inner.transform, "Title", "SETTINGS", 54, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
        UIKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(600f, 70f));

        Row(inner.transform, "MASTER VOLUME", -190f, true, GameSettings.Master, v => GameSettings.Master = v);
        Row(inner.transform, "SOUND EFFECTS", -280f, true, GameSettings.Sfx, v => { GameSettings.Sfx = v; });
        Row(inner.transform, "MUSIC", -370f, true, GameSettings.Music, v => { GameSettings.Music = v; });
        var label = UIKit.Label(inner.transform, "ShakeLabel", "SCREEN SHAKE", 28, UIKit.Lavender, TextAnchor.MiddleLeft);
        UIKit.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(50f, -460f), new Vector2(300f, 40f));
        var toggle = UIKit.MakeToggle(inner.transform, "ShakeToggle", new Vector2(48f, 48f), UIKit.Cyan, GameSettings.Shake, v => GameSettings.Shake = v);
        UIKit.Place((RectTransform)toggle.transform, new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-50f, -460f), new Vector2(48f, 48f));
        for (int i = 0; i < 3; i++)
        {
            var line = UIKit.Img(inner.transform, "Divider" + i, UIKit.White, new Color(0.3f, 0.5f, 1f, 0.25f));
            UIKit.Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -235f - i * 90f), new Vector2(620f, 2f));
        }

        var back = UIKit.MakeButton(inner.transform, "Back", "BACK", new Vector2(300f, 70f), new Color(0.12f, 0.2f, 0.5f, 1f), UIKit.Cyan, Color.white, 34, Close);
        UIKit.Place(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(300f, 70f));
    }

    static void Row(Transform parent, string label, float y, bool slider, float value, Action<float> onChanged)
    {
        var l = UIKit.Label(parent, label + "Label", label, 28, UIKit.Lavender, TextAnchor.MiddleLeft);
        UIKit.Place(l.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(50f, y), new Vector2(300f, 40f));
        var s = UIKit.MakeSlider(parent, label + "Slider", new Vector2(300f, 44f), UIKit.Cyan, value, onChanged);
        UIKit.Place((RectTransform)s.transform, new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-50f, y), new Vector2(300f, 44f));
    }
}

/// <summary>How-to-play modal: controls, objective and the six rules.</summary>
public class HowToPlayPanel : MonoBehaviour
{
    public static HowToPlayPanel Create(Transform parent)
    {
        var rt = UIKit.Rect("HowToPlayPanel", parent);
        UIKit.Stretch(rt);
        var panel = rt.gameObject.AddComponent<HowToPlayPanel>();
        panel.Build(rt);
        rt.gameObject.SetActive(false);
        return panel;
    }

    public bool IsOpen => gameObject.activeSelf;
    public void Open() => gameObject.SetActive(true);
    public void Close() => gameObject.SetActive(false);

    void Build(RectTransform root)
    {
        var dim = UIKit.Img(root, "Dim", UIKit.White, new Color(0.01f, 0.02f, 0.08f, 0.72f), true);
        UIKit.Stretch(dim.rectTransform);

        var inner = UIKit.Panel(root, "Window", UIKit.NavyDeep, UIKit.Border, out var outer, 22f, 3f, true);
        UIKit.Place(outer.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 760f));

        var title = UIKit.Label(inner.transform, "Title", "HOW TO PLAY", 54, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
        UIKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(800f, 70f));

        string cyan = UIKit.Hex(UIKit.Cyan), gold = UIKit.Hex(UIKit.Gold);
        // accent colours match the rule definitions
        string pink = UIKit.Hex(UIKit.Pink), red = UIKit.Hex(UIKit.Red), orange = UIKit.Hex(UIKit.Orange);
        string blue = UIKit.Hex(new Color(0.4f, 0.68f, 1f)), violet = UIKit.Hex(new Color(0.8f, 0.48f, 1f));

        var left = UIKit.Label(inner.transform, "Left",
            "<color=#" + cyan + ">CONTROLS</color>\n" +
            "WASD  \u2014  Move\nSPACE  \u2014  Jump\nESC  \u2014  Pause / Resume\nR  \u2014  Retry after game over\n\n" +
            "<color=#" + cyan + ">OBJECTIVE</color>\n" +
            "Survive as long as possible.\nCollect <color=#" + gold + ">coins</color> for bonus points.\nYou have three hearts. Rule violations\nand hazards cost health.",
            28, Color.white, TextAnchor.UpperLeft, FontStyle.Normal);
        left.lineSpacing = 1.2f;
        UIKit.Place(left.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -125f), new Vector2(470f, 520f));

        var divider = UIKit.Img(inner.transform, "ColumnDivider", UIKit.White, new Color(0.3f, 0.5f, 1f, 0.35f));
        UIKit.Place(divider.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(2f, 480f));

        var right = UIKit.Label(inner.transform, "Right",
            "<color=#" + cyan + ">THE RULES CHANGE</color>\n" +
            "<color=#" + pink + ">NEVER STOP</color>  \u2014  Keep moving.\n" +
            "<color=#" + red + ">RED IS DEAD</color>  \u2014  Avoid red tiles.\n" +
            "<color=#" + cyan + ">GRAVITY FLIP</color>  \u2014  Gravity reverses.\n" +
            "<color=#" + orange + ">THE FLOOR IS LAVA</color>  \u2014  Stand on\nraised safe platforms.\n" +
            "<color=#" + blue + ">KEEP JUMPING</color>  \u2014  Do not stay\ngrounded too long.\n" +
            "<color=#" + violet + ">CONTROLS REVERSED</color>  \u2014  Movement\ndirections reverse.",
            28, Color.white, TextAnchor.UpperLeft, FontStyle.Normal);
        right.lineSpacing = 1.2f;
        UIKit.Place(right.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(40f, -125f), new Vector2(480f, 520f));

        var back = UIKit.MakeButton(inner.transform, "Back", "BACK", new Vector2(300f, 70f), new Color(0.12f, 0.2f, 0.5f, 1f), UIKit.Cyan, Color.white, 34, Close);
        UIKit.Place(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(300f, 70f));
    }
}
