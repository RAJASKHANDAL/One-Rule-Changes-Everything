using System.Collections.Generic;
using UnityEngine;

public enum MutationPhase { NormalSurvival, RealityBreakWarning, MutationTransition, MutationWarning, MutationActive }

/// <summary>
/// Owns the NORMAL_SURVIVAL -> RULE_MUTATION phase change. The trigger is GameManager.SurvivalTime (the same scaled,
/// play-only clock the HUD shows - no second timer): the first gameplay update where it reaches MutationStartTime begins the
/// one-time Reality Break. Afterwards it schedules mutations (one at a time by default) on top of the normal rule system.
/// </summary>
public class MutationManager : MonoBehaviour
{
    public MutationBase[] mutations;
    public IntensityVisuals visuals;

    [Header("Trigger")]
    [Tooltip("Current-run survival time (seconds) at which the Reality Break begins.")]
    public float mutationStartTime = 150f;

    [Header("Reality Break")]
    public float introLineDuration = 1.6f;
    public float countdownDuration = 3f;
    public float activatedLabelDuration = 1.8f;
    [Tooltip("Max seconds to wait for a normal-rule announcement to finish before starting the sequence.")]
    public float maxWaitForRuleAnnouncement = 4f;
    public string[] introLines = { "YOU LEARNED THE RULES.", "NOW THE RULES CHANGE.", "REALITY BREAK INCOMING..." };

    [Header("Mutation schedule")]
    public float mutationWarning = 3.5f;
    public float minWarning = 3f;
    public float firstMutationDelay = 2f;
    public float minRecovery = 4f;
    public Vector2 gapIntroduction = new Vector2(14f, 18f);
    public Vector2 gapAdaptation = new Vector2(10f, 14f);
    public Vector2 gapMastery = new Vector2(8f, 11f);
    [Tooltip("Seconds after the Reality Break when ADAPTATION begins (180s by default).")]
    public float adaptationAfter = 30f;
    [Tooltip("Seconds after the Reality Break when MASTERY begins (240s by default).")]
    public float masteryAfter = 90f;
    [Min(1)] public int maxSimultaneousBeforeMastery = 1;
    [Min(1)] public int maxSimultaneousMastery = 2;
    [Tooltip("Validated pairs that may run together in MASTERY (ids joined with |).")]
    public string[] allowedPairs = { "ShiftingBlue|ChargingEnergy", "CursedCoins|ChargingEnergy" };

    [Header("Rewards (use the existing score system)")]
    public int survivalBonus = 100;
    public int streakBonus = 50;
    public int differentTypesBonus = 75;

    [Header("Debug")]
    public bool debugLogging;

    // ---- state
    public MutationPhase Phase { get; private set; } = MutationPhase.NormalSurvival;
    public bool RealityBreakTriggered { get; private set; }
    public int MutationsSurvived { get; private set; }
    public int TierIndex { get; private set; }
    public MutationBase ActiveMutation { get; private set; }

    /// <summary>Raised once per run when the Reality Break begins.</summary>
    public event System.Action RealityBreakStarted;

    // banner data polled by the UI
    public bool BannerVisible { get; private set; }
    public string BannerHeader { get; private set; } = "";
    public string BannerTitle { get; private set; } = "";
    public string BannerSub { get; private set; } = "";
    public int BannerCount { get; private set; }
    public Color BannerAccent { get; private set; } = Color.magenta;

    GameManager gm;
    int rbStage; // -1 waiting, 0..N-1 intro lines, N countdown, N+1 activated label
    float rbTimer, rbWait, bannerHold, nextMutationAt, lastEndTime = -99f;
    int streak;
    MutationBase lastStarted;
    readonly HashSet<MutationId> survivedTypes = new HashSet<MutationId>();
    readonly List<MutationBase> candidates = new List<MutationBase>();

    void OnValidate()
    {
        mutationStartTime = Mathf.Max(1f, mutationStartTime);
        introLineDuration = Mathf.Max(0.5f, introLineDuration);
        countdownDuration = Mathf.Max(1f, countdownDuration);
        activatedLabelDuration = Mathf.Max(0.5f, activatedLabelDuration);
        minWarning = Mathf.Max(1f, minWarning);
        mutationWarning = Mathf.Max(minWarning, mutationWarning);
        minRecovery = Mathf.Max(0f, minRecovery);
        adaptationAfter = Mathf.Max(1f, adaptationAfter);
        masteryAfter = Mathf.Max(adaptationAfter + 1f, masteryAfter);
    }

    void Start() { gm = GameManager.Instance; }

    void OnEnable()
    {
        var m = GameManager.Instance;
        if (m != null) m.StateChanged += OnStateChanged;
    }

    void OnDisable()
    {
        var m = GameManager.Instance;
        if (m != null) m.StateChanged -= OnStateChanged;
    }

    void Log(string msg) { if (debugLogging) Debug.Log("[Mutation] " + msg, this); }

    /// <summary>Game over / main menu: stop everything immediately and clean up.</summary>
    void OnStateChanged(GameState prev, GameState next)
    {
        if (next == GameState.GameOver || next == GameState.MainMenu) AbortAll();
    }

    public void AbortAll()
    {
        foreach (var m in mutations) m.ResetRun();
        ActiveMutation = null;
        if (gm == null) gm = GameManager.Instance;
        if (gm != null && gm.rules != null) gm.rules.SchedulingPaused = false;
        HideBanner();
        StatusBus.Clear("energy");
    }

    /// <summary>Full reset for a new run: back to NORMAL_SURVIVAL with a fresh threshold. Persistent data is untouched.</summary>
    public void ResetRun()
    {
        AbortAll();
        Phase = MutationPhase.NormalSurvival;
        RealityBreakTriggered = false;
        MutationsSurvived = 0; streak = 0; TierIndex = 0;
        survivedTypes.Clear();
        rbStage = -1; rbTimer = 0f; rbWait = 0f; bannerHold = 0f;
        nextMutationAt = float.MaxValue; lastEndTime = -99f; lastStarted = null;
        if (visuals != null) visuals.SetMutationLook(false);
    }

    // ------------------------------------------------------------------ banner helpers

    void ShowBanner(string header, string title, string sub, Color accent, int count = 0)
    {
        BannerVisible = true; BannerHeader = header; BannerTitle = title; BannerSub = sub; BannerAccent = accent; BannerCount = count;
    }

    void HideBanner() { BannerVisible = false; bannerHold = 0f; BannerCount = 0; }

    static readonly Color Magenta = new Color(0.85f, 0.3f, 1f);

    // ------------------------------------------------------------------ update

    void Update()
    {
        if (gm == null) gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return; // paused / menu / game over: nothing advances
        float dt = Time.deltaTime;
        float t = gm.SurvivalTime;

        if (Phase == MutationPhase.NormalSurvival)
        {
            if (!RealityBreakTriggered && t >= mutationStartTime) BeginRealityBreak(t);
            return;
        }
        if (Phase == MutationPhase.RealityBreakWarning) { TickRealityBreak(dt, t); return; }

        TickMutations(dt, t);
    }

    // ------------------------------------------------------------------ reality break

    void BeginRealityBreak(float t)
    {
        RealityBreakTriggered = true; // one-time flag set immediately
        Phase = MutationPhase.RealityBreakWarning;
        gm.rules.SchedulingPaused = true; // no NEW normal rules while the sequence plays
        rbStage = -1; rbWait = 0f;
        Log("Reality Break triggered at survival time " + t.ToString("0.00") + "s");
        RealityBreakStarted?.Invoke();
    }

    void TickRealityBreak(float dt, float t)
    {
        if (rbStage == -1)
        {
            // let an in-progress rule announcement finish first (never overlap two banners)
            rbWait += dt;
            if (gm.rules.HasPendingWarning && rbWait < maxWaitForRuleAnnouncement) return;
            rbStage = 0; rbTimer = introLineDuration;
            ShowIntro();
            AudioManager.Play(Sfx.RealityBreak);
            if (visuals != null) visuals.Pulse(1f);
            return;
        }

        rbTimer -= dt;
        int lines = introLines.Length;

        if (rbStage < lines)
        {
            if (rbTimer <= 0f)
            {
                rbStage++;
                if (rbStage < lines) { rbTimer = introLineDuration; ShowIntro(); }
                else { rbTimer = countdownDuration; ShowCountdown(); }
            }
        }
        else if (rbStage == lines) // countdown
        {
            ShowCountdown();
            if (rbTimer <= 0f)
            {
                rbStage++;
                EnterMutationPhase(t);
            }
        }
        else // "RULE MUTATION ACTIVATED" label
        {
            if (rbTimer <= 0f) { HideBanner(); Phase = MutationPhase.MutationTransition; }
        }
    }

    void ShowIntro()
    {
        ShowBanner("REALITY BREAK", introLines[Mathf.Clamp(rbStage, 0, introLines.Length - 1)], "", Magenta, 0);
        if (visuals != null) visuals.Pulse(0.35f);
    }

    int lastCount;
    void ShowCountdown()
    {
        int c = Mathf.Max(1, Mathf.CeilToInt(rbTimer));
        ShowBanner("REALITY BREAK", "REALITY BREAK INCOMING...", "", Magenta, c);
        if (c != lastCount) { lastCount = c; AudioManager.Play(Sfx.Click, 0.6f); }
    }

    void EnterMutationPhase(float t)
    {
        rbTimer = activatedLabelDuration;
        ShowBanner("REALITY BREAK", "RULE MUTATION ACTIVATED", "THE RULES NOW MEAN SOMETHING ELSE", Magenta, 0);
        Phase = MutationPhase.RealityBreakWarning; // stays until the label has been shown; the label stage handles the exit
        AudioManager.Play(Sfx.Mutation);
        if (visuals != null) { visuals.Pulse(1.2f); visuals.SetMutationLook(true); }
        gm.cameraRig.Shake(0.35f, 0.4f);
        gm.rules.SchedulingPaused = false;                 // normal scheduling resumes under the mutation-phase config
        nextMutationAt = t + firstMutationDelay + activatedLabelDuration;
        bannerHold = 0f;
        Log("Phase -> RULE_MUTATION");
    }

    // ------------------------------------------------------------------ mutations

    void UpdateTier(float t)
    {
        float since = t - mutationStartTime;
        TierIndex = since >= masteryAfter ? 2 : since >= adaptationAfter ? 1 : 0;
    }

    int ActiveCount() { int n = 0; foreach (var m in mutations) if (m.IsActive) n++; return n; }
    bool AnyWarning() { foreach (var m in mutations) if (m.IsWarning) return true; return false; }

    void TickMutations(float dt, float t)
    {
        UpdateTier(t);

        if (bannerHold > 0f)
        {
            bannerHold -= dt;
            if (bannerHold <= 0f && !AnyWarning()) HideBanner();
        }

        foreach (var m in mutations)
        {
            if (m.IsWarning)
            {
                m.WarnRemaining -= dt;
                BannerCount = Mathf.Max(1, Mathf.CeilToInt(m.WarnRemaining));
                if (BannerCount != lastCount) { lastCount = BannerCount; AudioManager.Play(Sfx.Click, 0.5f); }
                if (m.WarnRemaining <= 0f) ActivateMutation(m);
            }
            else if (m.IsActive)
            {
                m.Tick(dt);
                if (gm.State != GameState.Playing) return; // the tick may have ended the run
                m.Remaining -= dt;
                if (m.Remaining <= 0f) FinishMutation(m, t);
            }
        }

        ActiveMutation = null;
        foreach (var m in mutations) if (m.IsActive) { ActiveMutation = m; break; }
        Phase = AnyWarning() ? MutationPhase.MutationWarning : ActiveMutation != null ? MutationPhase.MutationActive : MutationPhase.MutationTransition;

        if (t >= nextMutationAt && !AnyWarning() && t - lastEndTime >= minRecovery && ActiveCount() < MaxSimultaneous())
            TryStartMutation(t);
    }

    int MaxSimultaneous() => TierIndex >= 2 ? maxSimultaneousMastery : maxSimultaneousBeforeMastery;

    Vector2 GapForTier() => TierIndex >= 2 ? gapMastery : TierIndex == 1 ? gapAdaptation : gapIntroduction;

    bool PairAllowed(MutationBase a, MutationBase b)
    {
        string x = a.id + "|" + b.id, y = b.id + "|" + a.id;
        foreach (var p in allowedPairs) if (p == x || p == y) return true;
        return false;
    }

    bool Eligible(MutationBase m)
    {
        if (m.IsActive || m.IsWarning || m.minTier > TierIndex || m.weight <= 0f || !m.CanStart()) return false;
        if (!string.IsNullOrEmpty(m.baseRuleId) && !gm.rules.IsEligibleBase(m.baseRuleId)) return false;
        foreach (var other in mutations)
        {
            if (!other.IsActive) continue;
            if (!PairAllowed(m, other)) return false; // incompatible combinations are rejected
            if (!string.IsNullOrEmpty(m.baseRuleId) && m.baseRuleId == other.baseRuleId) return false;
        }
        return true;
    }

    void TryStartMutation(float t)
    {
        candidates.Clear();
        float total = 0f;
        foreach (var m in mutations) if (Eligible(m)) candidates.Add(m);
        if (candidates.Count > 1 && lastStarted != null) candidates.Remove(lastStarted); // avoid immediate repeats
        if (candidates.Count == 0) { nextMutationAt = t + 2f; return; }

        foreach (var m in candidates) total += m.weight;
        float pick = Random.value * total;
        var chosen = candidates[candidates.Count - 1];
        foreach (var m in candidates) { pick -= m.weight; if (pick <= 0f) { chosen = m; break; } }
        StartWarning(chosen, t);
    }

    void StartWarning(MutationBase m, float t)
    {
        float warning = Mathf.Max(minWarning, mutationWarning);
        if (!string.IsNullOrEmpty(m.baseRuleId))
        {
            if (!gm.rules.QueueForMutation(m.baseRuleId, warning)) { nextMutationAt = t + 2f; return; }
            gm.rules.EnsureRemaining(m.baseRuleId, warning + m.duration + 2f); // base rule must outlive the mutation
        }
        m.WarnRemaining = warning;
        m.BeginWarning();
        lastStarted = m;
        lastCount = -1;
        ShowBanner("RULE MUTATION INCOMING", m.headline, m.description, m.accent, Mathf.CeilToInt(warning));
        bannerHold = 0f;
        AudioManager.Play(Sfx.Warning);
        nextMutationAt = t + Random.Range(GapForTier().x, GapForTier().y);
        Log("Warning: " + m.id);
    }

    void ActivateMutation(MutationBase m)
    {
        if (!string.IsNullOrEmpty(m.baseRuleId) && !gm.rules.IsRuleActive(m.baseRuleId))
        {
            m.CancelWarning(); // base rule vanished (e.g. restart): drop the mutation safely
            HideBanner();
            nextMutationAt = gm.SurvivalTime + 2f;
            return;
        }
        float scale = TierIndex >= 2 ? 1.2f : TierIndex == 1 ? 1.1f : 1f;
        m.Remaining = m.duration * scale;
        m.DamageAtStart = gm.health.DamageEvents;
        if (!string.IsNullOrEmpty(m.baseRuleId)) gm.rules.EnsureRemaining(m.baseRuleId, m.Remaining + 1f);
        m.Activate();
        ShowBanner("MUTATED RULE ACTIVE", m.headline, m.description, m.accent, 0);
        bannerHold = activatedLabelDuration;
        AudioManager.Play(Sfx.Mutation);
        if (visuals != null) visuals.Pulse(0.8f);
        Log("Active: " + m.id);
    }

    void FinishMutation(MutationBase m, float t)
    {
        bool noDamage = gm.health.DamageEvents == m.DamageAtStart;
        m.Deactivate();
        lastEndTime = t;
        nextMutationAt = Mathf.Max(nextMutationAt, t + minRecovery);
        Log("Finished: " + m.id);
        if (gm.State != GameState.Playing) return; // never reward after the run ended

        MutationsSurvived++;
        gm.AddBonus("MUTATION SURVIVED", survivalBonus);
        if (noDamage)
        {
            streak++;
            if (streak >= 2) gm.AddBonus("MUTATION STREAK x" + streak, streakBonus);
        }
        else streak = 0;
        if (survivedTypes.Add(m.id) && survivedTypes.Count >= 2)
            gm.AddBonus("DIFFERENT MUTATIONS x" + survivedTypes.Count, differentTypesBonus);
    }

    /// <summary>Test hook: start a specific mutation now (same path as the scheduler, but ignoring tier and timing).</summary>
    public bool DebugStartMutation(MutationId id)
    {
        if (gm == null) gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing || Phase == MutationPhase.NormalSurvival || Phase == MutationPhase.RealityBreakWarning) return false;
        foreach (var m in mutations)
        {
            if (m.id != id || m.IsActive || m.IsWarning || AnyWarning() || !m.CanStart()) continue;
            if (!string.IsNullOrEmpty(m.baseRuleId) && !gm.rules.IsEligibleBase(m.baseRuleId)) return false;
            StartWarning(m, gm.SurvivalTime);
            return m.IsWarning;
        }
        return false;
    }
}
