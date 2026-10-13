using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>In-game HUD: health, survival time / score / coins, active rules panel, status line and damage flash.</summary>
public class GameplayHUD : MonoBehaviour
{
    const int RowCount = 4;

    class RuleRow
    {
        public GameObject Root; public Image Stripe, Badge, Bar; public Text Glyph, Name, Time;
        public int LastSeconds = -1;
        public RuleBase Rule;
    }

    GameManager gm;
    RuleManager rules;
    PlayerHealth health;
    Image[] hearts;
    Text timeText, scoreText, coinText, statusText;
    GameObject rulesPanel;
    Image flash;
    readonly RuleRow[] rows = new RuleRow[RowCount];
    readonly StringBuilder sb = new StringBuilder(64);
    int lastSec = -1, lastScore = -1, lastCoins = -1;
    float flashAlpha;
    int lastHealth = -1;
    float heartPunch;
    Image lowEdge, intensityFill;
    float shownIntensity;
    const int BonusLineCount = 3;
    readonly Text[] bonusLines = new Text[BonusLineCount];
    readonly float[] bonusAge = new float[BonusLineCount];
    readonly string[] bonusText = new string[BonusLineCount];

    public static GameplayHUD Create(Transform parent)
    {
        var rt = UIKit.Rect("HUD", parent);
        UIKit.Stretch(rt);
        rt.gameObject.SetActive(false); // so OnEnable runs after Build
        var hud = rt.gameObject.AddComponent<GameplayHUD>();
        hud.Build(rt);
        return hud;
    }

    public void Show(bool visible) => gameObject.SetActive(visible);

    void Build(RectTransform root)
    {
        gm = GameManager.Instance;
        rules = gm.rules;
        health = gm.health;

        // damage flash (full screen vignette)
        flash = UIKit.Img(root, "DamageFlash", UIKit.Vignette, new Color(1f, 0.1f, 0.15f, 0f));
        UIKit.Stretch(flash.rectTransform);

        lowEdge = UIKit.Img(root, "LowHealthEdge", UIKit.Vignette, new Color(1f, 0.1f, 0.2f, 0f));
        UIKit.Stretch(lowEdge.rectTransform);

        BuildHealth(root);
        BuildIntensity(root);
        BuildBonusFeed(root);
        BuildTopBar(root);
        BuildRulesPanel(root);

        statusText = UIKit.Label(root, "Status", "", 58, UIKit.Red, TextAnchor.MiddleCenter, FontStyle.Bold, true);
        UIKit.Place(statusText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(1400f, 180f));
        statusText.verticalOverflow = VerticalWrapMode.Overflow;
        var outline = statusText.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0.05f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);
    }

    void BuildIntensity(RectTransform root)
    {
        var inner = UIKit.Panel(root, "Intensity", UIKit.Navy, UIKit.Border, out var outer, 10f, 2f);
        UIKit.Place(outer.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -128f), new Vector2(60f + 72f * health.maxHealth, 46f));
        var label = UIKit.Label(inner.transform, "Label", "INTENSITY", 16, UIKit.Cyan, TextAnchor.MiddleLeft);
        UIKit.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -4f), new Vector2(200f, 18f));
        var barBg = UIKit.Img(inner.transform, "BarBg", UIKit.White, new Color(0f, 0f, 0.1f, 0.7f));
        barBg.rectTransform.anchorMin = Vector2.zero; barBg.rectTransform.anchorMax = new Vector2(1f, 0f);
        barBg.rectTransform.offsetMin = new Vector2(14f, 7f); barBg.rectTransform.offsetMax = new Vector2(-14f, 15f);
        intensityFill = UIKit.Img(barBg.transform, "Fill", UIKit.White, UIKit.Cyan);
        UIKit.Stretch(intensityFill.rectTransform);
        intensityFill.type = Image.Type.Filled; intensityFill.fillMethod = Image.FillMethod.Horizontal;
    }

    void BuildBonusFeed(RectTransform root)
    {
        for (int i = 0; i < BonusLineCount; i++)
        {
            var t = UIKit.Label(root, "Bonus" + i, "", 24, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, true);
            UIKit.Place(t.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -186f - i * 32f), new Vector2(520f, 30f));
            t.color = new Color(1f, 0.82f, 0.22f, 0f);
            bonusLines[i] = t;
            bonusAge[i] = 99f;
        }
    }

    void OnBonus(string label, int points)
    {
        for (int i = BonusLineCount - 1; i > 0; i--) { bonusText[i] = bonusText[i - 1]; bonusAge[i] = bonusAge[i - 1]; }
        bonusText[0] = "+" + points + "  " + label;
        bonusAge[0] = 0f;
    }

    void ClearTransient()
    {
        for (int i = 0; i < BonusLineCount; i++) { bonusAge[i] = 99f; bonusText[i] = ""; if (bonusLines[i] != null) bonusLines[i].color = new Color(1f, 0.82f, 0.22f, 0f); }
        flashAlpha = 0f; heartPunch = 0f; lastHealth = -1;
        if (flash != null) flash.color = new Color(1f, 0.1f, 0.15f, 0f);
        if (lowEdge != null) lowEdge.color = new Color(1f, 0.1f, 0.2f, 0f);
        shownIntensity = gm != null && gm.intensity != null ? gm.intensity.Value : 0f;
        OnHealthChanged(health.Current, health.maxHealth);
    }

    void BuildHealth(RectTransform root)
    {
        var inner = UIKit.Panel(root, "Health", UIKit.Navy, UIKit.Border, out var outer, 16f);
        UIKit.Place(outer.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(60f + 72f * health.maxHealth, 92f));
        hearts = new Image[health.maxHealth];
        for (int i = 0; i < hearts.Length; i++)
        {
            hearts[i] = UIKit.Img(inner.transform, "Heart" + i, UIKit.Heart, Color.white);
            UIKit.Place(hearts[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(26f + 72f * i, 0f), new Vector2(60f, 60f));
        }
    }

    void BuildTopBar(RectTransform root)
    {
        var inner = UIKit.Panel(root, "TopBar", UIKit.Navy, UIKit.Border, out var outer, 16f);
        UIKit.Place(outer.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(720f, 100f));

        timeText = Column(inner.transform, 0, "SURVIVAL TIME", "00:00", Color.white, UIKit.Cyan);
        scoreText = Column(inner.transform, 1, "SCORE", "0", Color.white, UIKit.Cyan);
        coinText = Column(inner.transform, 2, "COINS", "0", UIKit.Gold, UIKit.Cyan);

        for (int i = 1; i <= 2; i++)
        {
            var d = UIKit.Img(inner.transform, "Divider" + i, UIKit.White, new Color(0.3f, 0.55f, 1f, 0.5f));
            UIKit.Place(d.rectTransform, new Vector2(i / 3f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2f, 62f));
        }
        var coinIcon = UIKit.Img(inner.transform, "CoinIcon", UIKit.Circle, UIKit.Gold);
        UIKit.Place(coinIcon.rectTransform, new Vector2(2f / 3f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(46f, -14f), new Vector2(34f, 34f));
        coinText.rectTransform.anchoredPosition += new Vector2(18f, 0f);
    }

    static Text Column(Transform parent, int index, string label, string value, Color valueColor, Color labelColor)
    {
        var col = UIKit.Rect("Col" + index, parent);
        col.anchorMin = new Vector2(index / 3f, 0f); col.anchorMax = new Vector2((index + 1) / 3f, 1f);
        col.offsetMin = col.offsetMax = Vector2.zero;
        var l = UIKit.Label(col, "Label", label, 20, labelColor, TextAnchor.MiddleCenter);
        UIKit.Place(l.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(240f, 28f));
        var v = UIKit.Label(col, "Value", value, 46, valueColor, TextAnchor.MiddleCenter, FontStyle.Bold, true);
        UIKit.Place(v.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(240f, 56f));
        return v;
    }

    void BuildRulesPanel(RectTransform root)
    {
        var outer = UIKit.Img(root, "ActiveRules", UIKit.Rounded, UIKit.Border);
        outer.type = Image.Type.Sliced; outer.pixelsPerUnitMultiplier = 24f / 16f;
        UIKit.Place(outer.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(390f, 100f));
        var vlg0 = outer.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg0.padding = new RectOffset(2, 2, 2, 2);
        vlg0.childControlWidth = vlg0.childControlHeight = true;
        vlg0.childForceExpandWidth = vlg0.childForceExpandHeight = false;
        var fit0 = outer.gameObject.AddComponent<ContentSizeFitter>();
        fit0.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var fill = UIKit.Img(outer.transform, "Fill", UIKit.Rounded, UIKit.Navy);
        fill.type = Image.Type.Sliced; fill.pixelsPerUnitMultiplier = 24f / 14f;
        var vlg = fill.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 12, 14);
        vlg.spacing = 8f;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        var header = UIKit.Label(fill.transform, "Header", "ACTIVE RULES", 22, UIKit.Cyan, TextAnchor.MiddleLeft);
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

        for (int i = 0; i < RowCount; i++) rows[i] = BuildRow(fill.transform, i);
        rulesPanel = outer.gameObject;
        rulesPanel.SetActive(false);
    }

    RuleRow BuildRow(Transform parent, int i)
    {
        var row = new RuleRow();
        var bg = UIKit.Img(parent, "Row" + i, UIKit.Rounded, new Color(0.1f, 0.14f, 0.32f, 0.9f));
        bg.type = Image.Type.Sliced; bg.pixelsPerUnitMultiplier = 24f / 10f;
        bg.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
        row.Root = bg.gameObject;

        row.Stripe = UIKit.Img(bg.transform, "Stripe", UIKit.White, UIKit.Cyan);
        row.Stripe.rectTransform.anchorMin = Vector2.zero; row.Stripe.rectTransform.anchorMax = new Vector2(0f, 1f);
        row.Stripe.rectTransform.sizeDelta = new Vector2(6f, 0f); row.Stripe.rectTransform.pivot = new Vector2(0f, 0.5f);

        row.Badge = UIKit.Img(bg.transform, "Badge", UIKit.Circle, UIKit.Cyan);
        UIKit.Place(row.Badge.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 3f), new Vector2(40f, 40f));
        row.Glyph = UIKit.Label(row.Badge.transform, "Glyph", "!", 24, new Color(0.03f, 0.05f, 0.14f), TextAnchor.MiddleCenter);
        UIKit.Stretch(row.Glyph.rectTransform);

        row.Name = UIKit.Label(bg.transform, "Name", "RULE", 24, Color.white, TextAnchor.MiddleLeft);
        UIKit.Place(row.Name.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(70f, 5f), new Vector2(220f, 34f));
        row.Time = UIKit.Label(bg.transform, "Time", "0s", 26, UIKit.Lavender, TextAnchor.MiddleRight);
        UIKit.Place(row.Time.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, 5f), new Vector2(80f, 34f));

        var barBg = UIKit.Img(bg.transform, "BarBg", UIKit.White, new Color(0f, 0f, 0.1f, 0.6f));
        UIKit.Place(barBg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(-28f, 5f));
        barBg.rectTransform.anchorMin = new Vector2(0f, 0f); barBg.rectTransform.anchorMax = new Vector2(1f, 0f);
        barBg.rectTransform.offsetMin = new Vector2(16f, 6f); barBg.rectTransform.offsetMax = new Vector2(-14f, 11f);
        row.Bar = UIKit.Img(barBg.transform, "Bar", UIKit.White, UIKit.Cyan);
        UIKit.Stretch(row.Bar.rectTransform);
        row.Bar.type = Image.Type.Filled; row.Bar.fillMethod = Image.FillMethod.Horizontal;

        bg.gameObject.SetActive(false);
        return row;
    }

    void OnEnable()
    {
        if (health != null) { health.HealthChanged += OnHealthChanged; health.Damaged += OnDamaged; OnHealthChanged(health.Current, health.maxHealth); }
        StatusBus.Changed += RefreshStatus;
        if (gm != null) { gm.BonusAwarded += OnBonus; gm.RunReset += ClearTransient; }
        RefreshStatus();
    }

    void OnDisable()
    {
        if (health != null) { health.HealthChanged -= OnHealthChanged; health.Damaged -= OnDamaged; }
        StatusBus.Changed -= RefreshStatus;
        if (gm != null) { gm.BonusAwarded -= OnBonus; gm.RunReset -= ClearTransient; }
    }

    void OnHealthChanged(int current, int max)
    {
        for (int i = 0; i < hearts.Length; i++)
            hearts[i].color = i < current ? new Color(1f, 0.25f, 0.42f) : new Color(0.22f, 0.15f, 0.32f, 0.85f);
        if (lastHealth >= 0 && current < lastHealth) heartPunch = 1f;
        lastHealth = current;
    }

    void OnDamaged(string source) { flashAlpha = 0.85f; }

    void RefreshStatus()
    {
        if (statusText == null) return;
        sb.Clear();
        foreach (var key in StatusBus.Keys)
        {
            var e = StatusBus.Get(key);
            if (sb.Length > 0) sb.Append('\n');
            sb.Append("<color=#").Append(UIKit.Hex(e.Color)).Append('>').Append(e.Text).Append("</color>");
        }
        statusText.text = sb.ToString();
    }

    void Update()
    {
        float udt = Time.unscaledDeltaTime;

        int sec = Mathf.FloorToInt(gm.SurvivalTime);
        if (sec != lastSec) { lastSec = sec; timeText.text = (sec / 60).ToString("00") + ":" + (sec % 60).ToString("00"); }
        if (gm.Score != lastScore) { lastScore = gm.Score; scoreText.text = lastScore.ToString(); }
        if (gm.Coins != lastCoins) { lastCoins = gm.Coins; coinText.text = lastCoins.ToString(); }

        if (flashAlpha > 0f)
        {
            flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, udt * 1.6f);
            flash.color = new Color(1f, 0.1f, 0.15f, flashAlpha);
        }
        UpdateHearts(udt);
        UpdateIntensity(udt);
        UpdateBonusFeed(udt);

        if (statusText.text.Length > 0)
        {
            float pulse = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 14f);
            statusText.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
        }

        UpdateRules();
    }

    void UpdateHearts(float udt)
    {
        if (heartPunch > 0f) heartPunch = Mathf.MoveTowards(heartPunch, 0f, udt * 3f);
        bool low = health.IsLow && !health.IsDead;
        float pulse = low ? 1f + 0.14f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f)) : 1f;
        for (int i = 0; i < hearts.Length; i++)
        {
            float s = 1f;
            if (heartPunch > 0f && i == Mathf.Max(0, lastHealth)) s *= 1f + heartPunch * 0.35f;
            if (low && i == 0) s *= pulse;
            hearts[i].rectTransform.localScale = Vector3.one * s;
        }
        // restrained red edge glow while on the last heart (never covers the centre)
        float target = low ? 0.16f + 0.1f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f)) : 0f;
        var c = lowEdge.color;
        c.a = Mathf.MoveTowards(c.a, target, udt * 0.8f);
        lowEdge.color = c;
    }

    void UpdateIntensity(float udt)
    {
        float v = gm.intensity != null ? gm.intensity.Value : 0f;
        shownIntensity = Mathf.MoveTowards(shownIntensity, v, udt * 0.25f);
        intensityFill.fillAmount = Mathf.Clamp01(shownIntensity);
        intensityFill.color = shownIntensity < 0.5f
            ? Color.Lerp(UIKit.Cyan, UIKit.Pink, shownIntensity * 2f)
            : Color.Lerp(UIKit.Pink, UIKit.Red, (shownIntensity - 0.5f) * 2f);
    }

    void UpdateBonusFeed(float udt)
    {
        for (int i = 0; i < BonusLineCount; i++)
        {
            bonusAge[i] += udt;
            float a = bonusAge[i] < 0.15f ? bonusAge[i] / 0.15f : Mathf.Clamp01((2.6f - bonusAge[i]) / 0.6f);
            if (bonusAge[i] > 3f) { if (bonusLines[i].text.Length > 0) bonusLines[i].text = ""; continue; }
            if (bonusLines[i].text != bonusText[i]) bonusLines[i].text = bonusText[i];
            var c = bonusLines[i].color; c.a = a; bonusLines[i].color = c;
        }
    }

    void UpdateRules()
    {
        int shown = 0;
        for (int i = 0; i < rules.SlotCount && shown < RowCount; i++)
        {
            if (!rules.TryGetSlot(i, out var rule, out bool active, out float remaining, out float total) || !active) continue;
            var row = rows[shown++];
            var d = rule.definition;
            if (!row.Root.activeSelf || row.Rule != rule)
            {
                row.Rule = rule;
                row.Root.SetActive(true);
                row.Name.text = d.displayName;
                row.Glyph.text = d.glyph;
                row.Stripe.color = row.Badge.color = row.Bar.color = d.accent;
                row.LastSeconds = -1;
            }
            int secs = Mathf.CeilToInt(remaining);
            if (secs != row.LastSeconds) { row.LastSeconds = secs; row.Time.text = secs + "s"; }
            row.Bar.fillAmount = total > 0f ? Mathf.Clamp01(remaining / total) : 0f;
        }
        for (int i = shown; i < RowCount; i++) if (rows[i].Root.activeSelf) rows[i].Root.SetActive(false);
        rulesPanel.SetActive(shown > 0);
    }
}
