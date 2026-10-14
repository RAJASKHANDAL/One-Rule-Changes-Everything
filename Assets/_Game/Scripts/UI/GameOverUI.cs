using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    GameManager gm;
    Text timeValue, scoreValue, coinsValue, rulesValue, bestScoreValue, bestTimeValue, comboValue, missionsValue, intensityValue, masteredValue, message;
    RectTransform newBest;
    float shownAt;

    public static GameOverUI Create(Transform parent, CosmeticsPanel cosmetics)
    {
        var rt = UIKit.Rect("GameOver", parent);
        UIKit.Stretch(rt);
        var ui = rt.gameObject.AddComponent<GameOverUI>();
        ui.Build(rt, cosmetics);
        rt.gameObject.SetActive(false);
        return ui;
    }

    static string Clock(float seconds)
    {
        int s = Mathf.FloorToInt(seconds);
        return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
    }

    public void Show(bool visible)
    {
        gameObject.SetActive(visible);
        if (!visible) return;
        shownAt = Time.unscaledTime;
        timeValue.text = Clock(gm.SurvivalTime);
        scoreValue.text = gm.Score.ToString("N0");
        coinsValue.text = gm.Coins.ToString();
        rulesValue.text = gm.RulesSurvived.ToString();
        bestScoreValue.text = gm.BestScore.ToString("N0");
        bestTimeValue.text = Clock(gm.BestTime);
        comboValue.text = gm.BestCombo > 1 ? "x" + gm.BestCombo : "-";
        missionsValue.text = gm.MissionsCompleted + " / " + gm.missions.missionCount;
        intensityValue.text = Mathf.RoundToInt(gm.HighestIntensity * 100f) + "%";
        masteredValue.text = gm.MutationsSurvived.ToString();
        message.text = gm.ContextMessage;
        newBest.gameObject.SetActive(gm.NewBest);
    }

    void Build(RectTransform root, CosmeticsPanel cosmetics)
    {
        gm = GameManager.Instance;
        var dim = UIKit.Img(root, "Dim", UIKit.White, new Color(0.03f, 0.01f, 0.08f, 0.8f), true);
        UIKit.Stretch(dim.rectTransform);

        var title = UIKit.Label(root, "Title", "GAME OVER", 130, new Color(1f, 0.3f, 0.45f), TextAnchor.MiddleCenter, FontStyle.Bold, true);
        UIKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1400f, 150f));
        var outline = title.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.5f, 0.05f, 0.2f, 0.9f);
        outline.effectDistance = new Vector2(4f, -4f);

        message = UIKit.Label(root, "Message", "", 32, UIKit.Cyan, TextAnchor.MiddleCenter, FontStyle.Bold, true);
        UIKit.Place(message.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -205f), new Vector2(1100f, 44f));

        var inner = UIKit.Panel(root, "Stats", UIKit.NavyDeep, UIKit.Border, out var outer, 18f, 3f);
        UIKit.Place(outer.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -30f), new Vector2(680f, 500f));
        timeValue = StatRow(inner.transform, 0, "SURVIVAL TIME", Color.white);
        scoreValue = StatRow(inner.transform, 1, "FINAL SCORE", Color.white);
        coinsValue = StatRow(inner.transform, 2, "COINS COLLECTED", UIKit.Gold);
        rulesValue = StatRow(inner.transform, 3, "RULES SURVIVED", UIKit.Cyan);
        bestScoreValue = SmallRow(inner.transform, 0, "BEST SCORE", UIKit.Gold);
        bestTimeValue = SmallRow(inner.transform, 1, "BEST TIME", UIKit.Gold);
        comboValue = SmallRow(inner.transform, 2, "BEST COIN COMBO", UIKit.Orange);
        missionsValue = SmallRow(inner.transform, 3, "MISSIONS COMPLETED", UIKit.Cyan);
        intensityValue = SmallRow(inner.transform, 4, "PEAK INTENSITY", UIKit.Pink);
        masteredValue = SmallRow(inner.transform, 5, "MUTATIONS SURVIVED", new Color(0.85f, 0.4f, 1f));

        var badge = UIKit.Panel(outer.transform, "NewBest", new Color(1f, 0.7f, 0.15f, 1f), new Color(1f, 0.9f, 0.5f), out var badgeBorder, 10f, 2f);
        newBest = badgeBorder.rectTransform;
        UIKit.Place(newBest, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-30f, 10f), new Vector2(190f, 46f));
        newBest.localRotation = Quaternion.Euler(0f, 0f, 6f);
        var badgeText = UIKit.Label(badge.transform, "Text", "NEW BEST!", 26, new Color(0.15f, 0.07f, 0f), TextAnchor.MiddleCenter);
        UIKit.Stretch(badgeText.rectTransform);

        var retry = UIKit.MakeButton(root, "Retry", "PLAY AGAIN", new Vector2(480f, 92f), new Color(1f, 0.25f, 0.45f, 1f), new Color(1f, 0.65f, 0.8f), Color.white, 50, gm.StartRun);
        UIKit.Place(retry.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(480f, 92f));
        var navy = new Color(0.08f, 0.12f, 0.3f, 0.95f);
        var menu = UIKit.MakeButton(root, "MainMenu", "MAIN MENU", new Vector2(300f, 60f), navy, UIKit.Border, Color.white, 28, gm.GoToMainMenu);
        UIKit.Place(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-160f, 70f), new Vector2(300f, 60f));
        var coll = UIKit.MakeButton(root, "Collection", "COLLECTION", new Vector2(300f, 60f), navy, UIKit.Border, Color.white, 28, cosmetics.Open);
        UIKit.Place(coll.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(160f, 70f), new Vector2(300f, 60f));
        var hint = UIKit.Label(root, "Hint", "PRESS R TO PLAY AGAIN", 20, UIKit.Lavender, TextAnchor.MiddleCenter, FontStyle.Normal);
        UIKit.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(500f, 28f));
    }

    static Text SmallRow(Transform parent, int index, string label, Color valueColor)
    {
        float x = index % 2 == 0 ? 40f : -40f;
        float anchorX = index % 2 == 0 ? 0f : 1f;
        float y = -308f - (index / 2) * 62f;
        var align = index % 2 == 0 ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
        var l = UIKit.Label(parent, "SmallLabel" + index, label, 17, UIKit.Lavender, align, FontStyle.Normal);
        UIKit.Place(l.rectTransform, new Vector2(anchorX, 1f), new Vector2(anchorX, 1f), new Vector2(x, y), new Vector2(290f, 22f));
        var v = UIKit.Label(parent, "SmallValue" + index, "0", 30, valueColor, align, FontStyle.Bold, true);
        UIKit.Place(v.rectTransform, new Vector2(anchorX, 1f), new Vector2(anchorX, 1f), new Vector2(x, y - 22f), new Vector2(290f, 34f));
        return v;
    }

    static Text StatRow(Transform parent, int index, string label, Color valueColor)
    {
        float y = -34f - index * 62f;
        var l = UIKit.Label(parent, "Label" + index, label, 28, UIKit.Lavender, TextAnchor.MiddleLeft);
        UIKit.Place(l.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(40f, y - 26f), new Vector2(360f, 52f));
        var v = UIKit.Label(parent, "Value" + index, "0", 42, valueColor, TextAnchor.MiddleRight, FontStyle.Bold, true);
        UIKit.Place(v.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(-40f, y - 26f), new Vector2(240f, 52f));
        if (index > 0)
        {
            var line = UIKit.Img(parent, "Line" + index, UIKit.White, new Color(0.3f, 0.5f, 1f, 0.3f));
            UIKit.Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, y + 31f), new Vector2(600f, 2f));
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
