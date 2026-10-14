using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Banner for the Reality Break sequence and mutation warnings ("RULE MUTATION INCOMING", "MUTATED RULE ACTIVE").
/// Purely presentational: it reads the banner fields exposed by MutationManager each frame and animates in / out.
/// </summary>
public class MutationAnnouncementUI : MonoBehaviour
{
    MutationManager mutations;
    RectTransform banner;
    CanvasGroup group;
    Image border, icon;
    Text header, title, sub, count, bang;
    string lastHeader, lastTitle, lastSub;
    int lastCount = -1;
    float appearT;
    bool wasVisible;

    public bool Visible => wasVisible;

    public static MutationAnnouncementUI Create(Transform parent, MutationManager mutations)
    {
        var rt = UIKit.Rect("MutationAnnouncement", parent);
        UIKit.Stretch(rt);
        rt.gameObject.SetActive(false);
        var ui = rt.gameObject.AddComponent<MutationAnnouncementUI>();
        ui.mutations = mutations;
        ui.Build(rt);
        rt.gameObject.SetActive(true);
        return ui;
    }

    void Build(RectTransform root)
    {
        var inner = UIKit.Panel(root, "Banner", UIKit.NavyDeep, new Color(0.85f, 0.3f, 1f), out border, 18f, 4f);
        banner = border.rectTransform;
        // sits in the upper-middle, below the normal rule banner area, so it never covers the player for long
        UIKit.Place(banner, new Vector2(0.5f, 0.74f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 170f));
        group = banner.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.alpha = 0f;

        icon = UIKit.Img(inner.transform, "Icon", UIKit.Triangle, new Color(0.85f, 0.3f, 1f));
        UIKit.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(26f, 4f), new Vector2(92f, 92f));
        bang = UIKit.Label(icon.transform, "Bang", "!", 54, new Color(0.05f, 0.03f, 0.14f), TextAnchor.MiddleCenter);
        UIKit.Stretch(bang.rectTransform, 0, 0, 22, 0);

        header = UIKit.Label(inner.transform, "Header", "", 20, UIKit.Lavender, TextAnchor.MiddleLeft, FontStyle.Bold);
        UIKit.Place(header.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(142f, -10f), new Vector2(560f, 24f));
        title = UIKit.Label(inner.transform, "Title", "", 52, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold, true);
        UIKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140f, -38f), new Vector2(560f, 72f));
        UIKit.Fit(title, 22, 48);
        sub = UIKit.Label(inner.transform, "Sub", "", 26, UIKit.Lavender, TextAnchor.MiddleLeft);
        UIKit.Place(sub.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(142f, 16f), new Vector2(560f, 34f));
        UIKit.Fit(sub, 16, 26);
        count = UIKit.Label(inner.transform, "Count", "", 90, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
        UIKit.Place(count.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(150f, 130f));
        UIKit.Fit(count, 40, 90);
    }

    void Update()
    {
        if (mutations == null) return;
        float dt = Time.unscaledDeltaTime;
        bool visible = mutations.BannerVisible;

        if (visible)
        {
            if (!wasVisible) appearT = 0f;
            appearT += dt;
            var accent = mutations.BannerAccent;
            border.color = icon.color = accent;
            if (mutations.BannerHeader != lastHeader) { lastHeader = mutations.BannerHeader; header.text = lastHeader; }
            if (mutations.BannerTitle != lastTitle) { lastTitle = mutations.BannerTitle; title.text = lastTitle; appearT = Mathf.Min(appearT, 0.12f); }
            if (mutations.BannerSub != lastSub) { lastSub = mutations.BannerSub; sub.text = lastSub; }
            if (mutations.BannerCount != lastCount) { lastCount = mutations.BannerCount; count.text = lastCount > 0 ? lastCount.ToString() : ""; }

            float a = Mathf.Clamp01(appearT / 0.25f);
            group.alpha = a;
            banner.localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, 1f - (1f - a) * (1f - a));
            icon.rectTransform.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(Time.unscaledTime * 12f));
        }
        else if (group.alpha > 0f)
        {
            group.alpha = Mathf.MoveTowards(group.alpha, 0f, dt * 5f);
            banner.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, group.alpha);
        }
        wasVisible = visible;
    }
}
