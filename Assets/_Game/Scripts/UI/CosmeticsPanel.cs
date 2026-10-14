using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Collection screen: shows every cosmetic as locked / unlocked / equipped, lets the player buy (exactly once) and equip.</summary>
public class CosmeticsPanel : MonoBehaviour
{
    class Card { public CosmeticItem Item; public Button Button; public Text Label; public Image Swatch; }

    readonly List<Card> cards = new List<Card>();
    Text walletText, messageText;
    float messageTime;

    public static CosmeticsPanel Create(Transform parent)
    {
        var rt = UIKit.Rect("CosmeticsPanel", parent);
        UIKit.Stretch(rt);
        var panel = rt.gameObject.AddComponent<CosmeticsPanel>();
        panel.Build(rt);
        rt.gameObject.SetActive(false);
        return panel;
    }

    public bool IsOpen => gameObject.activeSelf;
    public void Open() { gameObject.SetActive(true); Refresh(); }
    public void Close() { gameObject.SetActive(false); }

    void OnEnable() { CosmeticManager.Changed += Refresh; Wallet.Changed += Refresh; }
    void OnDisable() { CosmeticManager.Changed -= Refresh; Wallet.Changed -= Refresh; }

    void Build(RectTransform root)
    {
        var dim = UIKit.Img(root, "Dim", UIKit.White, new Color(0.01f, 0.02f, 0.08f, 0.78f), true);
        UIKit.Stretch(dim.rectTransform);
        var inner = UIKit.Panel(root, "Window", UIKit.NavyDeep, UIKit.Border, out var outer, 22f, 3f, true);
        UIKit.Place(outer.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1120f, 720f));

        var title = UIKit.Label(inner.transform, "Title", "COLLECTION", 52, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold, true);
        UIKit.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(50f, -28f), new Vector2(600f, 66f));
        walletText = UIKit.Label(inner.transform, "Wallet", "", 34, UIKit.Gold, TextAnchor.MiddleRight, FontStyle.Bold, true);
        UIKit.Place(walletText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-50f, -34f), new Vector2(420f, 54f));

        string[] names = { "SCARF", "HOOD", "TRAIL", "COIN SPARKS" };
        for (int c = 0; c < 4; c++)
        {
            float y = -160f - c * 128f;
            var label = UIKit.Label(inner.transform, "Cat" + c, names[c], 24, UIKit.Cyan, TextAnchor.MiddleLeft);
            UIKit.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(50f, y - 50f), new Vector2(200f, 40f));
            int slot = 0;
            foreach (var item in CosmeticManager.All)
            {
                if ((int)item.Category != c) continue;
                var captured = item;
                var btn = UIKit.MakeButton(inner.transform, "Item_" + item.Id, "", new Vector2(250f, 100f), new Color(0.08f, 0.12f, 0.3f, 0.95f), UIKit.Border, Color.white, 22, () => OnCard(captured));
                UIKit.Place(btn.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(270f + slot * 266f, y - 50f), new Vector2(250f, 100f));
                var swatch = UIKit.Img(btn.transform, "Swatch", UIKit.Circle, item.Color.a > 0f ? item.Color : new Color(0.3f, 0.3f, 0.4f, 0.6f));
                UIKit.Place(swatch.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(34f, 34f));
                var text = btn.GetComponentInChildren<Text>();
                text.alignment = TextAnchor.MiddleLeft;
                text.rectTransform.offsetMin = new Vector2(62f, 0f);
                cards.Add(new Card { Item = item, Button = btn, Label = text, Swatch = swatch });
                slot++;
            }
        }

        messageText = UIKit.Label(inner.transform, "Message", "", 26, UIKit.Red, TextAnchor.MiddleCenter);
        UIKit.Place(messageText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 104f), new Vector2(700f, 36f));
        var back = UIKit.MakeButton(inner.transform, "Back", "BACK", new Vector2(300f, 70f), new Color(0.12f, 0.2f, 0.5f, 1f), UIKit.Cyan, Color.white, 34, Close);
        UIKit.Place(back.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(300f, 70f));
    }

    void OnCard(CosmeticItem item)
    {
        if (CosmeticManager.IsUnlocked(item.Id)) { CosmeticManager.Equip(item.Id); return; }
        if (CosmeticManager.TryBuy(item.Id)) { CosmeticManager.Equip(item.Id); Say("UNLOCKED " + item.Name, UIKit.Cyan); }
        else Say("NOT ENOUGH COINS", UIKit.Red);
    }

    void Say(string text, Color color) { messageText.text = text; messageText.color = color; messageTime = 2f; }

    void Update()
    {
        if (messageTime > 0f) { messageTime -= Time.unscaledDeltaTime; if (messageTime <= 0f) messageText.text = ""; }
    }

    void Refresh()
    {
        if (walletText == null) return;
        walletText.text = "COINS  " + Wallet.Coins;
        foreach (var c in cards)
        {
            bool unlocked = CosmeticManager.IsUnlocked(c.Item.Id);
            bool equipped = CosmeticManager.IsEquipped(c.Item.Id);
            string status = equipped ? "<color=#" + UIKit.Hex(UIKit.Cyan) + ">EQUIPPED</color>"
                : unlocked ? "EQUIP"
                : "<color=#" + UIKit.Hex(Wallet.Coins >= c.Item.Cost ? UIKit.Gold : UIKit.Red) + ">" + c.Item.Cost + " COINS</color>";
            c.Label.text = c.Item.Name + "\n" + status;
        }
    }
}
