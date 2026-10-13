using UnityEngine;
using UnityEngine.UI;

/// <summary>Large rule banner shown during the warning period, plus a small "rule ended" toast.</summary>
public class RuleAnnouncementUI : MonoBehaviour
{
    RuleManager rules;
    RectTransform banner, toast;
    CanvasGroup bannerGroup, toastGroup;
    Image border, toastBorder, bar, icon;
    Text nameText, subText, toastText, bang;
    RuleBase current;
    float bannerT = 1f;       // animation time since shown (unscaled)
    bool bannerShown;
    float toastTime = 99f;

    public static RuleAnnouncementUI Create(Transform parent)
    {
        var rt = UIKit.Rect("RuleAnnouncement", parent);
        UIKit.Stretch(rt);
        rt.gameObject.SetActive(false);
        var ui = rt.gameObject.AddComponent<RuleAnnouncementUI>();
        ui.Build(rt);
        rt.gameObject.SetActive(true);
        return ui;
    }

    void Build(RectTransform root)
    {
        rules = GameManager.Instance.rules;

        var inner = UIKit.Panel(root, "Banner", UIKit.NavyDeep, UIKit.Cyan, out border, 18f, 4f);
        banner = border.rectTransform;
        UIKit.Place(banner, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(720f, 136f));
        bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
        bannerGroup.blocksRaycasts = false;

        icon = UIKit.Img(inner.transform, "WarningIcon", UIKit.Triangle, UIKit.Cyan);
        UIKit.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(26f, 4f), new Vector2(92f, 92f));
        bang = UIKit.Label(icon.transform, "Bang", "!", 54, new Color(0.03f, 0.05f, 0.14f), TextAnchor.MiddleCenter);
        UIKit.Stretch(bang.rectTransform, 0, 0, 22, 0);

        nameText = UIKit.Label(inner.transform, "Name", "RULE", 60, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold, true);
        UIKit.Place(nameText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140f, -14f), new Vector2(560f, 76f));
        subText = UIKit.Label(inner.transform, "Sub", "INSTRUCTION", 28, UIKit.Lavender, TextAnchor.MiddleLeft);
        UIKit.Place(subText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(142f, 18f), new Vector2(560f, 36f));

        var barBg = UIKit.Img(inner.transform, "BarBg", UIKit.White, new Color(0f, 0f, 0.1f, 0.6f));
        barBg.rectTransform.anchorMin = Vector2.zero; barBg.rectTransform.anchorMax = new Vector2(1f, 0f);
        barBg.rectTransform.offsetMin = new Vector2(20f, 6f); barBg.rectTransform.offsetMax = new Vector2(-20f, 11f);
        bar = UIKit.Img(barBg.transform, "Bar", UIKit.White, UIKit.Cyan);
        UIKit.Stretch(bar.rectTransform);
        bar.type = Image.Type.Filled; bar.fillMethod = Image.FillMethod.Horizontal;

        var tInner = UIKit.Panel(root, "Toast", UIKit.Navy, UIKit.Cyan, out toastBorder, 12f, 2f);
        toast = toastBorder.rectTransform;
        UIKit.Place(toast, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(520f, 58f));
        toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
        toastGroup.blocksRaycasts = false;
        toastText = UIKit.Label(tInner.transform, "Text", "", 26, Color.white, TextAnchor.MiddleCenter);
        UIKit.Stretch(toastText.rectTransform);

        HideAll();
    }

    void OnEnable()
    {
        if (rules == null) return;
        rules.RuleWarning += OnWarning;
        rules.RuleActivated += OnActivated;
        rules.RuleEnded += OnEnded;
        rules.RulesCleared += HideAll;
    }

    void OnDisable()
    {
        if (rules == null) return;
        rules.RuleWarning -= OnWarning;
        rules.RuleActivated -= OnActivated;
        rules.RuleEnded -= OnEnded;
        rules.RulesCleared -= HideAll;
    }

    void HideAll()
    {
        current = null; bannerShown = false; toastTime = 99f;
        if (bannerGroup != null) { bannerGroup.alpha = 0f; toastGroup.alpha = 0f; }
    }

    void OnWarning(RuleBase rule, float duration)
    {
        current = rule;
        var d = rule.definition;
        nameText.text = d.displayName;
        subText.text = d.subtitle;
        border.color = icon.color = bar.color = d.accent;
        bannerShown = true; bannerT = 0f;
        toastGroup.alpha = 0f; toastTime = 99f;
    }

    void OnActivated(RuleBase rule)
    {
        if (rule == current) bannerShown = false; // fade out; rule now lives in the active rules panel
    }

    void OnEnded(RuleBase rule, bool completed)
    {
        var d = rule.definition;
        toastText.text = "<color=#" + UIKit.Hex(d.accent) + ">" + d.displayName + "</color> SURVIVED  <color=#" + UIKit.Hex(UIKit.Gold) + ">+" + GameManager.Instance.pointsPerRule + "</color>";
        toastBorder.color = d.accent;
        toastTime = 0f;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        if (bannerShown)
        {
            bannerT += dt;
            float a = Mathf.Clamp01(bannerT / 0.3f);
            float scale = 1f + 2.2f * Mathf.Pow(a - 1f, 3f) + 1.2f * Mathf.Pow(a - 1f, 2f); // ease-out-back
            banner.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(scale));
            bannerGroup.alpha = a;
            float pulse = 1f + 0.03f * Mathf.Sin(bannerT * 12f);
            icon.rectTransform.localScale = Vector3.one * pulse;
            if (current != null)
                for (int i = 0; i < rules.SlotCount; i++)
                    if (rules.TryGetSlot(i, out var r, out bool active, out float remaining, out float total) && r == current && !active)
                        bar.fillAmount = total > 0f ? remaining / total : 0f;
        }
        else if (bannerGroup.alpha > 0f)
        {
            bannerGroup.alpha = Mathf.MoveTowards(bannerGroup.alpha, 0f, dt * 5f);
            banner.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, bannerGroup.alpha); // quick scale-out on exit
        }

        if (toastTime < 2.2f)
        {
            toastTime += dt;
            toast.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, Mathf.Clamp01(toastTime / 0.15f));
            toastGroup.alpha = toastTime < 1.6f ? Mathf.Clamp01(toastTime / 0.12f) : Mathf.Clamp01((2.2f - toastTime) / 0.6f);
        }
        else if (toastGroup.alpha > 0f) toastGroup.alpha = 0f;
    }
}
