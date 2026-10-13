using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Owns the game state machine, run statistics, scoring (incl. skill bonuses) and run lifecycle (start / pause / game over / restart).</summary>
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public const string BestScoreKey = "OneRule.BestScore";
    public static GameManager Instance { get; private set; }

    [Header("Scene references")]
    public PlayerController player;
    public PlayerHealth health;
    public PlayerGravityController gravity;
    public RuleManager rules;
    public CoinSpawner coins;
    public CameraRig cameraRig;
    public ArenaController arena;
    public IntensityManager intensity;

    [Header("Scoring")]
    public float pointsPerSecond = 10f;
    public int pointsPerRule = 25;
    public int pointsPerCoin = 5;

    [Header("Skill bonuses")]
    public int ruleMasteryBonus = 50;
    public int comboBonus = 100;
    public int nearDeathBonus = 75;
    public float nearDeathSeconds = 10f;
    public int coinStreakBonus = 20;
    public int coinStreakLength = 5;
    public int maxCoinStreakBonuses = 10;

    public GameState State { get; private set; } = GameState.MainMenu;
    public float SurvivalTime { get; private set; }
    public int Score => Mathf.FloorToInt(scoreAccum);
    public int Coins { get; private set; }
    public int RulesSurvived { get; private set; }
    public int MasteredRules { get; private set; }
    public int BestCombo { get; private set; }
    public int BestScore { get; private set; }
    public bool NewBest { get; private set; }
    public float HighestIntensity => intensity != null ? intensity.Peak : 0f;

    public event Action<GameState, GameState> StateChanged;
    /// <summary>Raised after the world has been reset for a new run (or the menu). Listeners clear their temporary state.</summary>
    public event Action RunReset;
    /// <summary>Raised when a skill bonus is awarded: label, points.</summary>
    public event Action<string, int> BonusAwarded;

    float scoreAccum;
    int coinStreak, streakBonusesAwarded;
    float lowHealthTime;
    bool nearDeathAwarded;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        InputModifier.Clear();
        StatusBus.ClearAll();
        if (player == null || health == null || gravity == null || rules == null || coins == null || cameraRig == null || arena == null || intensity == null)
            Debug.LogError("GameManager: one or more scene references are missing.", this);
    }

    void Start()
    {
        EnterMainMenu();
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (State == GameState.Playing) Pause();
                else if (State == GameState.Paused) Resume();
            }
            if (kb.rKey.wasPressedThisFrame && State == GameState.GameOver) StartRun();
        }

        if (State == GameState.Playing)
        {
            float dt = Time.deltaTime;
            SurvivalTime += dt;
            scoreAccum += pointsPerSecond * dt;

            // near-death bonus: survive a while on the last heart (once per run)
            if (!nearDeathAwarded && health.Current == 1)
            {
                lowHealthTime += dt;
                if (lowHealthTime >= nearDeathSeconds)
                {
                    nearDeathAwarded = true;
                    AddBonus("NEAR-DEATH SURVIVAL", nearDeathBonus);
                }
            }
        }
    }

    static bool CanTransition(GameState from, GameState to)
    {
        if (from == to) return false;
        switch (from)
        {
            case GameState.MainMenu: return to == GameState.Playing;
            case GameState.Playing: return to == GameState.Paused || to == GameState.GameOver || to == GameState.MainMenu;
            case GameState.Paused: return to == GameState.Playing || to == GameState.MainMenu;
            case GameState.GameOver: return to == GameState.Playing || to == GameState.MainMenu;
        }
        return false;
    }

    bool SetState(GameState next)
    {
        if (!CanTransition(State, next)) return false;
        var prev = State;
        State = next;
        player.InputEnabled = next == GameState.Playing;
        cameraRig.SetMenuMode(next == GameState.MainMenu);
        StateChanged?.Invoke(prev, next);
        return true;
    }

    void EnterMainMenu()
    {
        ResetWorld();
        ResetStats();
        Time.timeScale = 1f;
        State = GameState.GameOver; // placeholder so the first transition notifies listeners
        SetState(GameState.MainMenu);
    }

    void ResetStats()
    {
        SurvivalTime = 0f; scoreAccum = 0f; Coins = 0; RulesSurvived = 0; NewBest = false;
        MasteredRules = 0; BestCombo = 0;
        coinStreak = 0; streakBonusesAwarded = 0; lowHealthTime = 0f; nearDeathAwarded = false;
    }

    void ResetWorld()
    {
        StatusBus.ClearAll();
        rules.ResetAll();
        gravity.ResetGravity();
        InputModifier.Clear();
        arena.ResetArena();
        health.ResetHealth();
        player.ResetPlayer();
        coins.ResetCoins();
        intensity.ResetRun();
        cameraRig.SnapToTarget();
    }

    public void StartRun()
    {
        if (State == GameState.Playing) return;
        ResetWorld();
        ResetStats();
        Time.timeScale = 1f;
        RunReset?.Invoke();
        SetState(GameState.Playing);
    }

    public void Pause()
    {
        if (State != GameState.Playing) return;
        Time.timeScale = 0f;
        SetState(GameState.Paused);
    }

    public void Resume()
    {
        if (State != GameState.Paused) return;
        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    public void GoToMainMenu()
    {
        if (State == GameState.MainMenu) return;
        Time.timeScale = 1f;
        ResetWorld();
        ResetStats();
        RunReset?.Invoke();
        SetState(GameState.MainMenu);
    }

    public void OnPlayerDied()
    {
        if (State != GameState.Playing) return;
        NewBest = Score > BestScore;
        if (NewBest)
        {
            BestScore = Score;
            PlayerPrefs.SetInt(BestScoreKey, BestScore);
            PlayerPrefs.Save();
        }
        SetState(GameState.GameOver);

        // Stop all rules and restore shared state; final statistics (incl. peak intensity) are preserved until the next run.
        StatusBus.ClearAll();
        rules.ResetAll();
        gravity.ResetGravity();
        InputModifier.Clear();
        arena.ResetArena();
        player.OnDeath();
        cameraRig.Shake(0.6f, 0.5f);
        AudioManager.Play(Sfx.GameOver);
    }

    // ------------------------------------------------------------------ scoring events

    public void AddCoin()
    {
        if (State != GameState.Playing) return;
        Coins++;
        scoreAccum += pointsPerCoin;
        coinStreak++;
        if (coinStreakLength > 0 && coinStreak % coinStreakLength == 0 && streakBonusesAwarded < maxCoinStreakBonuses)
        {
            streakBonusesAwarded++;
            AddBonus("COIN STREAK x" + coinStreak, coinStreakBonus);
        }
    }

    /// <summary>Called by PlayerHealth after damage was applied; breaks the coin streak.</summary>
    public void OnDamageTaken() { coinStreak = 0; }

    public void RuleCompleted()
    {
        if (State != GameState.Playing) return;
        RulesSurvived++;
        scoreAccum += pointsPerRule;
    }

    public void RuleMastered(RuleBase rule)
    {
        if (State != GameState.Playing) return;
        MasteredRules++;
        AddBonus("RULE MASTERY", ruleMasteryBonus);
    }

    public void ComboCompleted(int size)
    {
        if (State != GameState.Playing) return;
        BestCombo = Mathf.Max(BestCombo, size);
        AddBonus("COMBO SURVIVED", comboBonus);
    }

    public void AddBonus(string label, int points)
    {
        if (State != GameState.Playing || points <= 0) return;
        scoreAccum += points;
        AudioManager.Play(Sfx.Bonus);
        BonusAwarded?.Invoke(label, points);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
