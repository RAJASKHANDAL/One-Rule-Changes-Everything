using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    GameManager gm;
    Text timeValue, scoreValue, coinsValue, rulesValue, intensityValue, masteredValue, comboValue, bestValue;
    RectTransform newBest;
    float shownAt;

    public static GameOverUI Create(Transform parent)
    {
        var rt = UIKit.Rect("GameOver", parent);
        UIKit.Stretch(rt);
        var ui = rt.gameObject.AddComponent<GameOverUI>();
        ui.Build(rt);
        rt.gameObject.SetActive(false);
        return ui;
    }

    public void Show(bool visible)
    {
        gameObject.SetActive(visible);
        if (!visible) return;
        shownAt = Time.unscaledTime;
        int sec = Mathf.FloorToInt(gm.SurvivalTime);
        timeValue.text = (sec / 60).ToString("00") + ":" + (sec % 60).ToString("00");
        scoreValue.text = gm.Score.ToString("N0");
        coinsValue.text = gm.Coins.ToString();
        rulesValue.text = gm.RulesSurvived.ToString();
        intensityValue.text = Mathf.RoundToInt(gm.HighestIntensity * 100f) + "%";
        masteredValue.text = gm.MasteredRules.ToString();
        comboValue.text = gm.BestCombo > 0 ? "x" + gm.BestCombo : "-";
        bestValue.text = gm.BestScore.ToString("N0");
        newBest.gameObject.SetActive(gm.NewBest);
    }

    void Build(RectTransform root)
    {
        gm = GameManager.Instance;
        var dim = UIKit.Img(root, "Dim", UIKit.White, new Color(0.03f, 0.01f, 0.08f, 0.78f), true);
        UIKit.Stretch(dim.rectTransform);

        var title = UIKit.Label(root, "Title", "GAME OVER", 150, new Color(1f, 0.3f, 0.45f), TextAnchor.MiddleCenter, FontStyle.Bold, true);
        UIKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1400f, 180f));
        var outline = title.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.5f, 0.05f, 0.2f, 0.9f);
        outline.effectDistance = new Vector2(4f, -4f);

        var inner = UIKit.Panel(root, "Stats", UIKit.NavyDeep, UIKit.Border, out var outer, 18f, 3f);
        UIKit.Place(outer.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(640f, 450f));
        timeValue = StatRow(inner.transform, 0, "SURVIVAL TIME", Color.white);
        scoreValue = StatRow(inner.transform, 1, "FINAL SCORE", Color.white);
        coinsValue = StatRow(inner.transform, 2, "COINS COLLECTED", UIKit.Gold);
        rulesValue = StatRow(inner.transform, 3, "RULES SURVIVED", UIKit.Cyan);
        intensityValue = SmallRow(inner.transform, 0, "HIGHEST INTENSITY", UIKit.Pink);
        masteredValue = SmallRow(inner.transform, 1, "RULES MASTERED (NO DAMAGE)", UIKit.Cyan);
        comboValue = SmallRow(inner.transform, 2, "BEST COMBO SURVIVED", UIKit.Orange);
        bestValue = SmallRow(inner.transform, 3, "PERSONAL BEST", UIKit.Gold);

        var badge = UIKit.Panel(outer.transform, "NewBest", new Color(1f, 0.7f, 0.15f, 1f), new Color(1f, 0.9f, 0.5f), out var badgeBorder, 10f, 2f);
        newBest = badgeBorder.rectTransform;
        UIKit.Place(newBest, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-30f, 10f), new Vector2(190f, 46f));
        newBest.localRotation = Quaternion.Euler(0f, 0f, 6f);
        var badgeText = UIKit.Label(badge.transform, "Text", "NEW BEST!", 26, new Color(0.15f, 0.07f, 0f), TextAnchor.MiddleCenter);
        UIKit.Stretch(badgeText.rectTransform);

        var retry = UIKit.MakeButton(root, "Retry", "RETRY", new Vector2(460f, 100f), new Color(1f, 0.25f, 0.45f, 1f), new Color(1f, 0.65f, 0.8f), Color.white, 56, gm.StartRun);
        UIKit.Place(retry.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 210f), new Vector2(460f, 100f));
        var menu = UIKit.MakeButton(root, "MainMenu", "MAIN MENU", new Vector2(360f, 68f), new Color(0.08f, 0.12f, 0.3f, 0.95f), UIKit.Border, Color.white, 32, gm.GoToMainMenu);
        UIKit.Place(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(360f, 68f));
        var hint = UIKit.Label(root, "Hint", "PRESS R TO RETRY", 24, UIKit.Lavender, TextAnchor.MiddleCenter, FontStyle.Normal);
        UIKit.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(500f, 32f));
    }

    static Text SmallRow(Transform parent, int index, string label, Color valueColor)
    {
        float x = index % 2 == 0 ? 40f : -40f;
        float yy = -318f - (index / 2) * 56f;
        var anchorX = index % 2 == 0 ? 0f : 1f;
        var l = UIKit.Label(parent, "SmallLabel" + index, label, 17, UIKit.Lavender, TextAnchor.MiddleLeft, FontStyle.Normal);
        UIKit.Place(l.rectTransform, new Vector2(anchorX, 1f), new Vector2(anchorX, 1f), new Vector2(x, yy), new Vector2(280f, 22f));
        if (index % 2 == 1) l.alignment = TextAnchor.MiddleRight;
        var v = UIKit.Label(parent, "SmallValue" + index, "0", 30, valueColor, TextAnchor.MiddleLeft, FontStyle.Bold, true);
        UIKit.Place(v.rectTransform, new Vector2(anchorX, 1f), new Vector2(anchorX, 1f), new Vector2(x, yy - 22f), new Vector2(280f, 34f));
        if (index % 2 == 1) v.alignment = TextAnchor.MiddleRight;
        return v;
    }

    static Text StatRow(Transform parent, int index, string label, Color valueColor)
    {
        float y = -40f - index * 72f;
        var l = UIKit.Label(parent, "Label" + index, label, 30, UIKit.Lavender, TextAnchor.MiddleLeft);
        UIKit.Place(l.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(40f, y - 28f), new Vector2(340f, 56f));
        var v = UIKit.Label(parent, "Value" + index, "0", 44, valueColor, TextAnchor.MiddleRight, FontStyle.Bold, true);
        UIKit.Place(v.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(-40f, y - 28f), new Vector2(240f, 56f));
        if (index > 0)
        {
            var line = UIKit.Img(parent, "Line" + index, UIKit.White, new Color(0.3f, 0.5f, 1f, 0.3f));
            UIKit.Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, y + 36f), new Vector2(560f, 2f));
        }
        return v;
    }

    void Update()
    {
        float t = Mathf.Clamp01((Time.unscaledTime - shownAt) / 0.35f);
        transform.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, 1f - (1f - t) * (1f - t));
        if (newBest != null && newBest.gameObject.activeSelf)
            newBest.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(Time.unscaledTime * 6f));
    }
}
