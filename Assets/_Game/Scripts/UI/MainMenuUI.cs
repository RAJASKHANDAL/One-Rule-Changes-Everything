using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    Text bestText;
    GameManager gm;

    public static MainMenuUI Create(Transform parent, SettingsPanel settings, HowToPlayPanel howTo)
    {
        var rt = UIKit.Rect("MainMenu", parent);
        UIKit.Stretch(rt);
        var menu = rt.gameObject.AddComponent<MainMenuUI>();
        menu.Build(rt, settings, howTo);
        return menu;
    }

    public void Show(bool visible)
    {
        gameObject.SetActive(visible);
        if (visible && bestText != null) bestText.text = "BEST SCORE  <color=#" + UIKit.Hex(UIKit.Gold) + ">" + gm.BestScore + "</color>";
    }

    void Build(RectTransform root, SettingsPanel settings, HowToPlayPanel howTo)
    {
        gm = GameManager.Instance;

        var shade = UIKit.Img(root, "LeftShade", UIKit.Gradient, new Color(0.02f, 0.02f, 0.1f, 0.88f));
        shade.rectTransform.anchorMin = Vector2.zero; shade.rectTransform.anchorMax = new Vector2(0.62f, 1f);
        shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;

        var one = UIKit.Label(root, "TitleTop", "ONE RULE", 150, new Color(0.96f, 0.94f, 1f), TextAnchor.MiddleLeft, FontStyle.Bold, true);
        UIKit.Place(one.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(110f, -60f), new Vector2(1100f, 170f));
        var outline = one.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.4f, 0.2f, 0.9f, 0.9f);
        outline.effectDistance = new Vector2(4f, -4f);

        var changes = UIKit.Label(root, "TitleBottom",
            "<color=#" + UIKit.Hex(UIKit.Cyan) + ">CHANGES</color> <color=#" + UIKit.Hex(UIKit.Pink) + ">EVERYTHING</color>",
            76, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold, true);
        UIKit.Place(changes.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(114f, -218f), new Vector2(1100f, 100f));

        var changesOutline = changes.gameObject.AddComponent<Outline>();
        changesOutline.effectColor = new Color(0.25f, 0.1f, 0.6f, 0.9f);
        changesOutline.effectDistance = new Vector2(3f, -3f);

        var tag = UIKit.Label(root, "Tagline", "SURVIVE.  ADAPT.  BREAK THE RULES.", 30, UIKit.Orange, TextAnchor.MiddleLeft);
        UIKit.Place(tag.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(118f, -318f), new Vector2(900f, 44f));

        var play = UIKit.MakeButton(root, "Play", "PLAY", new Vector2(420f, 96f), new Color(1f, 0.25f, 0.45f, 1f), new Color(1f, 0.65f, 0.8f), Color.white, 52, () => gm.StartRun());
        UIKit.Place(play.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(120f, -110f), new Vector2(420f, 96f));

        var navy = new Color(0.08f, 0.12f, 0.3f, 0.92f);
        MenuButton(root, "Settings", "SETTINGS", -215f, navy, settings.Open);
        MenuButton(root, "HowToPlay", "HOW TO PLAY", -293f, navy, howTo.Open);
        MenuButton(root, "Quit", "QUIT", -371f, navy, gm.QuitGame);

        bestText = UIKit.Label(root, "Best", "", 30, UIKit.Lavender, TextAnchor.MiddleLeft);
        UIKit.Place(bestText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(120f, 50f), new Vector2(600f, 44f));
    }

    static void MenuButton(Transform parent, string name, string label, float y, Color fill, System.Action onClick)
    {
        var b = UIKit.MakeButton(parent, name, label, new Vector2(340f, 64f), fill, UIKit.Border, Color.white, 30, onClick);
        UIKit.Place(b.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(120f, y), new Vector2(340f, 64f));
    }
}
