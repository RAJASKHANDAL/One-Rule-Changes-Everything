using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Procedural UI building blocks (sprites, panels, labels, buttons, sliders) shared by every screen.</summary>
public static class UIKit
{
    public static readonly Color Navy = new Color(0.035f, 0.06f, 0.15f, 0.88f);
    public static readonly Color NavyDeep = new Color(0.02f, 0.035f, 0.1f, 0.94f);
    public static readonly Color Cyan = new Color(0.25f, 0.88f, 1f, 1f);
    public static readonly Color Pink = new Color(1f, 0.3f, 0.68f, 1f);
    public static readonly Color Red = new Color(1f, 0.27f, 0.32f, 1f);
    public static readonly Color Orange = new Color(1f, 0.62f, 0.2f, 1f);
    public static readonly Color Gold = new Color(1f, 0.82f, 0.22f, 1f);
    public static readonly Color Lavender = new Color(0.86f, 0.84f, 1f, 1f);
    public static readonly Color Border = new Color(0.25f, 0.5f, 0.98f, 0.9f);

    const float RoundedBorder = 24f;
    static Font font;
    static Sprite rounded, circle, triangle, heart, white, vignette, gradient;

    public static Font Font
    {
        get
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }
    }

    // ---------------------------------------------------------------- sprites

    static Sprite MakeSprite(int w, int h, Func<float, float, float> alphaAt, Vector4 border = default, int samples = 1)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float a = 0f;
                for (int sy = 0; sy < samples; sy++)
                    for (int sx = 0; sx < samples; sx++)
                        a += alphaAt(x + (sx + 0.5f) / samples, y + (sy + 0.5f) / samples);
                px[y * w + x] = new Color(1f, 1f, 1f, a / (samples * samples));
            }
        tex.SetPixels(px);
        tex.Apply();
        var s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        s.hideFlags = HideFlags.HideAndDontSave;
        return s;
    }

    public static Sprite White => white != null ? white : white = MakeSprite(4, 4, (x, y) => 1f);

    public static Sprite Rounded
    {
        get
        {
            if (rounded != null) return rounded;
            const int size = 64; const float r = 22f;
            rounded = MakeSprite(size, size, (x, y) =>
            {
                float px = Mathf.Abs(x - size * 0.5f) - (size * 0.5f - r);
                float py = Mathf.Abs(y - size * 0.5f) - (size * 0.5f - r);
                float d = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude + Mathf.Min(Mathf.Max(px, py), 0f) - r;
                return Mathf.Clamp01(0.5f - d);
            }, new Vector4(RoundedBorder, RoundedBorder, RoundedBorder, RoundedBorder));
            return rounded;
        }
    }

    public static Sprite Circle => circle != null ? circle : circle = MakeSprite(64, 64, (x, y) =>
        Mathf.Clamp01(0.5f - (new Vector2(x - 32f, y - 32f).magnitude - 31f)));

    public static Sprite Triangle => triangle != null ? triangle : triangle = MakeSprite(64, 64, (x, y) =>
    {
        // apex at top, rounded by inset
        Vector2 a = new Vector2(32f, 58f), b = new Vector2(4f, 8f), c = new Vector2(60f, 8f);
        float d = Mathf.Max(Edge(a, b, x, y), Mathf.Max(Edge(b, c, x, y), Edge(c, a, x, y)));
        return Mathf.Clamp01(0.5f - d);
    });

    static float Edge(Vector2 p0, Vector2 p1, float x, float y)
    {
        var e = p1 - p0;
        var n = new Vector2(e.y, -e.x).normalized; // outward for clockwise winding
        return Vector2.Dot(new Vector2(x, y) - p0, n);
    }

    public static Sprite Heart => heart != null ? heart : heart = MakeSprite(64, 64, (x, y) =>
    {
        float nx = (x - 32f) / 27f, ny = (y - 30f) / 27f;
        float f = Mathf.Pow(nx * nx + ny * ny - 1f, 3f) - nx * nx * ny * ny * ny;
        return f <= 0f ? 1f : 0f;
    }, default, 3);

    public static Sprite Vignette => vignette != null ? vignette : vignette = MakeSprite(128, 128, (x, y) =>
    {
        float d = new Vector2((x - 64f) / 64f, (y - 64f) / 64f).magnitude;
        return Mathf.SmoothStep(0.45f, 1.1f, d);
    });

    /// <summary>Horizontal gradient: opaque on the left, transparent on the right.</summary>
    public static Sprite Gradient => gradient != null ? gradient : gradient = MakeSprite(128, 4, (x, y) =>
        1f - Mathf.SmoothStep(0.2f, 1f, x / 128f));

    // ---------------------------------------------------------------- building blocks

    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    public static void Stretch(RectTransform rt, float l = 0, float r = 0, float t = 0, float b = 0)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
    }

    public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    public static Image Img(Transform parent, string name, Sprite sprite, Color color, bool raycast = false)
    {
        var rt = Rect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite; img.color = color; img.raycastTarget = raycast;
        return img;
    }

    /// <summary>Rounded panel with a thin coloured border. Returns the inner fill image; the border image is the parent-level object.</summary>
    public static Image Panel(Transform parent, string name, Color fill, Color border, out Image borderImage, float corner = 12f, float borderWidth = 2f, bool raycast = false)
    {
        borderImage = Img(parent, name, Rounded, border, raycast);
        borderImage.type = Image.Type.Sliced;
        borderImage.pixelsPerUnitMultiplier = RoundedBorder / corner;
        var inner = Img(borderImage.transform, "Fill", Rounded, fill);
        inner.type = Image.Type.Sliced;
        inner.pixelsPerUnitMultiplier = RoundedBorder / Mathf.Max(2f, corner - borderWidth);
        Stretch(inner.rectTransform, borderWidth, borderWidth, borderWidth, borderWidth);
        return inner;
    }

    public static Text Label(Transform parent, string name, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold, bool shadow = false)
    {
        var rt = Rect(name, parent);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = Font; t.text = text; t.fontSize = size; t.color = color;
        t.alignment = anchor; t.fontStyle = style; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.supportRichText = true;
        if (shadow)
        {
            var s = rt.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.65f);
            s.effectDistance = new Vector2(3f, -3f);
        }
        return t;
    }

    public static Button MakeButton(Transform parent, string name, string label, Vector2 size, Color fill, Color border, Color textColor, int fontSize, Action onClick)
    {
        var inner = Panel(parent, name, Color.white, border, out var outer, 14f, 3f, true);
        outer.rectTransform.sizeDelta = size;
        var btn = outer.gameObject.AddComponent<Button>();
        btn.targetGraphic = inner;
        var colors = btn.colors;
        colors.normalColor = fill;
        colors.highlightedColor = Color.Lerp(fill, Color.white, 0.25f);
        colors.pressedColor = Color.Lerp(fill, Color.black, 0.25f);
        colors.selectedColor = fill;
        colors.disabledColor = new Color(0.3f, 0.3f, 0.35f, 0.6f);
        colors.fadeDuration = 0.08f;
        btn.colors = colors;
        inner.color = Color.white;

        var text = Label(inner.transform, "Label", label, fontSize, textColor, TextAnchor.MiddleCenter, FontStyle.Bold, true);
        Stretch(text.rectTransform);

        outer.gameObject.AddComponent<ButtonFx>();
        btn.onClick.AddListener(() => { AudioManager.Play(Sfx.Click); onClick?.Invoke(); });
        return btn;
    }

    public static Slider MakeSlider(Transform parent, string name, Vector2 size, Color accent, float value, Action<float> onChanged)
    {
        var root = Rect(name, parent);
        root.sizeDelta = size;
        var slider = root.gameObject.AddComponent<Slider>();

        var bg = Img(root, "Background", Rounded, new Color(0.1f, 0.14f, 0.3f, 1f), true);
        bg.type = Image.Type.Sliced; bg.pixelsPerUnitMultiplier = RoundedBorder / 8f;
        bg.rectTransform.anchorMin = new Vector2(0f, 0.5f); bg.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        bg.rectTransform.sizeDelta = new Vector2(0f, 16f);

        var fillArea = Rect("Fill Area", root);
        fillArea.anchorMin = new Vector2(0f, 0.5f); fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.sizeDelta = new Vector2(-24f, 16f);
        var fill = Img(fillArea, "Fill", Rounded, accent);
        fill.type = Image.Type.Sliced; fill.pixelsPerUnitMultiplier = RoundedBorder / 8f;
        fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(0f, 1f);
        fill.rectTransform.sizeDelta = new Vector2(10f, 0f);

        var handleArea = Rect("Handle Slide Area", root);
        Stretch(handleArea, 12, 12, 0, 0);
        var handle = Img(handleArea, "Handle", Circle, Color.white, true);
        handle.rectTransform.sizeDelta = new Vector2(34f, 34f);

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f; slider.maxValue = 1f;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(v => onChanged?.Invoke(v));
        return slider;
    }

    public static Toggle MakeToggle(Transform parent, string name, Vector2 size, Color accent, bool isOn, Action<bool> onChanged)
    {
        var root = Rect(name, parent);
        root.sizeDelta = size;
        var toggle = root.gameObject.AddComponent<Toggle>();
        var bgInner = Panel(root, "Box", new Color(0.1f, 0.14f, 0.3f, 1f), Border, out var bgOuter, 8f, 2f, true);
        Stretch(bgOuter.rectTransform);
        var check = Img(bgInner.transform, "Check", Rounded, accent);
        check.type = Image.Type.Sliced; check.pixelsPerUnitMultiplier = RoundedBorder / 6f;
        Stretch(check.rectTransform, 8, 8, 8, 8);
        toggle.targetGraphic = bgOuter;
        toggle.graphic = check;
        toggle.SetIsOnWithoutNotify(isOn);
        toggle.onValueChanged.AddListener(v => { AudioManager.Play(Sfx.Click); onChanged?.Invoke(v); });
        return toggle;
    }

    public static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);
}

/// <summary>Hover / press scale feedback that keeps working while the game is paused.</summary>
public class ButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    float target = 1f;
    public void OnPointerEnter(PointerEventData e) => target = 1.05f;
    public void OnPointerExit(PointerEventData e) => target = 1f;
    public void OnPointerDown(PointerEventData e) => target = 0.96f;
    public void OnPointerUp(PointerEventData e) => target = 1.05f;
    void OnDisable() { target = 1f; transform.localScale = Vector3.one; }
    void Update()
    {
        float s = Mathf.Lerp(transform.localScale.x, target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
        transform.localScale = new Vector3(s, s, 1f);
    }
}
