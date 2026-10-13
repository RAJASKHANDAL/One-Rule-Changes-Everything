using UnityEngine;

public class PauseMenuUI : MonoBehaviour
{
    public static PauseMenuUI Create(Transform parent, SettingsPanel settings)
    {
        var rt = UIKit.Rect("PauseMenu", parent);
        UIKit.Stretch(rt);
        var pause = rt.gameObject.AddComponent<PauseMenuUI>();
        pause.Build(rt, settings);
        rt.gameObject.SetActive(false);
        return pause;
    }

    public void Show(bool visible) => gameObject.SetActive(visible);

    void Build(RectTransform root, SettingsPanel settings)
    {
        var gm = GameManager.Instance;
        var dim = UIKit.Img(root, "Dim", UIKit.White, new Color(0.01f, 0.02f, 0.08f, 0.6f), true);
        UIKit.Stretch(dim.rectTransform);

        var inner = UIKit.Panel(root, "Window", UIKit.NavyDeep, UIKit.Border, out var outer, 22f, 3f, true);
        UIKit.Place(outer.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 600f));

        var title = UIKit.Label(inner.transform, "Title", "PAUSED", 60, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
        UIKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(460f, 80f));

        var resume = UIKit.MakeButton(inner.transform, "Resume", "RESUME", new Vector2(380f, 84f), new Color(1f, 0.25f, 0.45f, 1f), new Color(1f, 0.65f, 0.8f), Color.white, 44, gm.Resume);
        UIKit.Place(resume.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(380f, 84f));

        var navy = new Color(0.08f, 0.12f, 0.3f, 0.95f);
        Option(inner.transform, "Restart", "RESTART", -260f, navy, gm.StartRun);
        Option(inner.transform, "Settings", "SETTINGS", -340f, navy, settings.Open);
        Option(inner.transform, "MainMenu", "MAIN MENU", -420f, navy, gm.GoToMainMenu);
    }

    static void Option(Transform parent, string name, string label, float y, Color fill, System.Action onClick)
    {
        var b = UIKit.MakeButton(parent, name, label, new Vector2(340f, 64f), fill, UIKit.Border, Color.white, 30, onClick);
        UIKit.Place(b.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(340f, 64f));
    }
}
