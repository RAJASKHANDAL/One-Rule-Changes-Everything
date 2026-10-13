using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>Builds the whole UI at runtime (canvas, EventSystem, every screen) and switches screens with the game state.</summary>
public class GameUI : MonoBehaviour
{
    public float gameOverDelay = 1.1f;

    GameManager gm;
    GameplayHUD hud;
    MainMenuUI mainMenu;
    PauseMenuUI pauseMenu;
    GameOverUI gameOver;
    SettingsPanel settings;
    HowToPlayPanel howTo;
    Coroutine gameOverRoutine;

    void Awake()
    {
        gm = GameManager.Instance;

        var canvasGo = new GameObject("Canvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        var root = (RectTransform)canvasGo.transform;
        hud = GameplayHUD.Create(root);
        RuleAnnouncementUI.Create(hud.transform);
        settings = SettingsPanel.Create(root);
        howTo = HowToPlayPanel.Create(root);
        // modals must render above the screens that open them, so build screens first and re-order
        mainMenu = MainMenuUI.Create(root, settings, howTo);
        pauseMenu = PauseMenuUI.Create(root, settings);
        gameOver = GameOverUI.Create(root);
        settings.transform.SetAsLastSibling();
        howTo.transform.SetAsLastSibling();
    }

    void OnEnable()
    {
        if (gm != null) gm.StateChanged += OnStateChanged;
    }

    void OnDisable()
    {
        if (gm != null) gm.StateChanged -= OnStateChanged;
    }

    void Start() { Apply(gm.State); }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame && gm.State == GameState.MainMenu)
        {
            if (settings.IsOpen) settings.Close();
            else if (howTo.IsOpen) howTo.Close();
        }
    }

    void OnStateChanged(GameState prev, GameState next) => Apply(next);

    void Apply(GameState state)
    {
        if (gameOverRoutine != null) { StopCoroutine(gameOverRoutine); gameOverRoutine = null; }
        settings.Close();
        howTo.Close();

        hud.Show(state == GameState.Playing || state == GameState.Paused);
        mainMenu.Show(state == GameState.MainMenu);
        pauseMenu.Show(state == GameState.Paused);
        gameOver.Show(false);
        if (state == GameState.GameOver) gameOverRoutine = StartCoroutine(ShowGameOver());
    }

    IEnumerator ShowGameOver()
    {
        yield return new WaitForSecondsRealtime(gameOverDelay);
        if (gm.State == GameState.GameOver) gameOver.Show(true);
        gameOverRoutine = null;
    }
}
