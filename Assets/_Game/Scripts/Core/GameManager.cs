using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Owns the game state machine, run statistics (the run summary), scoring incl. skill bonuses, and the run lifecycle
/// (start / pause / game over / restart). Feature managers (rules, intensity, combo, missions, ...) are referenced, not duplicated.
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public const string BestScoreKey = "OneRule.BestScore";
    public const string BestTimeKey = "OneRule.BestTime";
    public const string RunsKey = "OneRule.Runs";
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
    public CoinComboManager combo;
    public NearMissDetector nearMiss;
    public MissionManager missions;
    public MutationManager mutations;

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
    [Tooltip("Score multiplier while on the last heart (risk reward, kept small so it never feels like a requirement).")]
    [Range(1f, 1.5f)] public float clutchScoreMultiplier = 1.25f;

    public GameState State { get; private set; } = GameState.MainMenu;
    public float SurvivalTime { get; private set; }
    public int Score => Mathf.FloorToInt(scoreAccum);
    public int Coins { get; private set; }
    public int RulesSurvived { get; private set; }
    public int RulesSinceDamage { get; private set; }
    public int MasteredRules { get; private set; }
    public int MissionsCompleted => missions != null ? missions.CompletedCount : 0;
    public int BestCombo => combo != null ? combo.BestCombo : 0;
    public int MutationsSurvived => mutations != null ? mutations.MutationsSurvived : 0;
    public int BestScore { get; private set; }
    public float BestTime { get; private set; }
    public bool NewBest { get; private set; }
    public bool NewBestTime { get; private set; }
    public bool WasFirstRun { get; private set; }
    public float HighestIntensity => intensity != null ? intensity.Peak : 0f;
    public float ScoreMultiplier => health != null && health.IsLow && !health.IsDead ? clutchScoreMultiplier : 1f;
    public string ContextMessage { get; private set; } = "";

    public event Action<GameState, GameState> StateChanged;
    /// <summary>Raised after the world has been reset for a new run (or the menu). Listeners clear their temporary state.</summary>
    public event Action RunReset;
    /// <summary>Raised when a skill bonus is awarded: label, points.</summary>
    public event Action<string, int> BonusAwarded;
    /// <summary>Raised once per collected coin with the coin's world position.</summary>
    public event Action<Vector3> CoinCollected;

    float scoreAccum;
    int coinStreak, streakBonusesAwarded;
    float lowHealthTime;
    bool nearDeathAwarded;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        BestTime = PlayerPrefs.GetFloat(BestTimeKey, 0f);
        InputModifier.Clear();
        StatusBus.ClearAll();
        if (player == null || health == null || gravity == null || rules == null || coins == null || cameraRig == null || arena == null
            || intensity == null || combo == null || nearMiss == null || missions == null || mutations == null)
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
            scoreAccum += pointsPerSecond * dt * ScoreMultiplier;

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
        SurvivalTime = 0f; scoreAccum = 0f; Coins = 0; RulesSurvived = 0; RulesSinceDamage = 0; NewBest = false; NewBestTime = false;
        MasteredRules = 0; ContextMessage = "";
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
        combo.ResetRun();
        nearMiss.ResetRun();
        mutations.ResetRun();
        cameraRig.SnapToTarget();
    }

    public void StartRun()
    {
        if (State == GameState.Playing) return;
        ResetWorld();
        ResetStats();
        missions.ResetRun();
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
        missions.ResetRun();
        RunReset?.Invoke();
        SetState(GameState.MainMenu);
    }

    public void OnPlayerDied()
    {
        if (State != GameState.Playing) return;

        WasFirstRun = PlayerPrefs.GetInt(RunsKey, 0) == 0;
        PlayerPrefs.SetInt(RunsKey, PlayerPrefs.GetInt(RunsKey, 0) + 1);
        NewBest = Score > BestScore;
        if (NewBest) { BestScore = Score; PlayerPrefs.SetInt(BestScoreKey, BestScore); }
        NewBestTime = SurvivalTime > BestTime;
        if (NewBestTime) { BestTime = SurvivalTime; PlayerPrefs.SetFloat(BestTimeKey, BestTime); }
        PlayerPrefs.Save();
        ContextMessage = BuildContextMessage();
        SetState(GameState.GameOver);

        // Stop all rules and restore shared state; the run summary (incl. peak intensity) is preserved until the next run.
        StatusBus.ClearAll();
        rules.ResetAll();
        gravity.ResetGravity();
        InputModifier.Clear();
        arena.ResetArena();
        player.OnDeath();
        cameraRig.Shake(0.6f, 0.5f);
        AudioManager.Play(NewBest ? Sfx.NewBest : Sfx.GameOver);
    }

    string BuildContextMessage()
    {
        if (WasFirstRun) return "First run. Learn the rules.";
        if (NewBest) return "New personal best!";
        if (SurvivalTime >= 90f) return "You survived the chaos!";
        if (MissionsCompleted >= missions.missionCount) return "Every mission done. Brilliant adapting!";
        if (SurvivalTime >= 45f) return "Getting faster!";
        return "Every run teaches a rule. Go again!";
    }

    // ------------------------------------------------------------------ scoring events

    /// <summary>Collects a coin. <paramref name="valueMultiplier"/> is 1 for normal coins and higher for risky coins.</summary>
    public void AddCoin(int valueMultiplier = 1, Vector3 position = default)
    {
        if (State != GameState.Playing) return;
        Coins++;
        Wallet.Add(valueMultiplier);

        int basePoints = pointsPerCoin * Mathf.Max(1, valueMultiplier);
        int comboBonusPoints = combo.RegisterCoin(basePoints);
        scoreAccum += (basePoints + comboBonusPoints) * ScoreMultiplier;
        if (comboBonusPoints > 0) BonusAwarded?.Invoke("COIN COMBO x" + combo.Count, comboBonusPoints);

        CoinCollected?.Invoke(position);
        coinStreak++;
        if (coinStreakLength > 0 && coinStreak % coinStreakLength == 0 && streakBonusesAwarded < maxCoinStreakBonuses)
        {
            streakBonusesAwarded++;
            AddBonus("COIN STREAK x" + coinStreak, coinStreakBonus);
        }
    }

    /// <summary>Called by PlayerHealth after damage was applied; breaks the coin streak and the no-damage rule counter.</summary>
    public void OnDamageTaken() { coinStreak = 0; RulesSinceDamage = 0; }

    public void RuleCompleted()
    {
        if (State != GameState.Playing) return;
        RulesSurvived++;
        RulesSinceDamage++;
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
        AddBonus("RULE COMBO SURVIVED", comboBonus);
    }

    public void OnMissionCompleted(Mission mission)
    {
        if (State != GameState.Playing) return;
        Wallet.Add(mission.RewardCoins);
        AddBonus("MISSION COMPLETE", mission.RewardScore);
    }

    public void AddBonus(string label, int points)
    {
        if (State != GameState.Playing || points <= 0) return;
        scoreAccum += points;
        AudioManager.Play(Sfx.Bonus);
        BonusAwarded?.Invoke(label, points);
    }

#if UNITY_EDITOR
    /// <summary>Editor-only test hook: sets the current-run survival time (used by the automated play-through).</summary>
    public void DebugSetSurvivalTime(float seconds) { SurvivalTime = seconds; }
#endif

    public void QuitGame()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
