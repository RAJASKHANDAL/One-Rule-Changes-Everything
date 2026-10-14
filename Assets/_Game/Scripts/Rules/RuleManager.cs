using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Schedules rules from the run intensity, runs their warning/active timers, enforces compatibility and the
/// explicit combo allow-list, tracks mastery / combos for scoring, and cleans everything up on reset.
/// </summary>
public class RuleManager : MonoBehaviour
{
    public RuleBase[] rules;

    [Header("Schedule (start-to-start interval per intensity band)")]
    public float firstRuleTime = 16f;
    public Vector2 earlyInterval = new Vector2(12f, 15f);
    public Vector2 midInterval = new Vector2(8f, 11f);
    public Vector2 lateInterval = new Vector2(5f, 8f);
    [Tooltip("Minimum quiet time after a rule ends before a rule may start into an empty arena.")]
    public float minRestAfterRule = 2.5f;

    [Header("Concurrency / combos (never more than 2 without separate balancing)")]
    public int maxConcurrentEarly = 1;
    public int maxConcurrentMid = 2;
    public int maxConcurrentLate = 2;
    [Tooltip("Chance of adding a compatible second rule, indexed by run phase (Learn, FirstChanges, Adaptation, HighPressure, Mastery).")]
    public float[] comboChanceByPhase = { 0f, 0.25f, 0.4f, 0.65f, 0.8f };
    [Min(1)] public int maxSimultaneousRules = 2;
    [Tooltip("Pairs (ruleIdA|ruleIdB) that were checked against the implementations and may run together.")]
    public string[] comboPairs =
    {
        "never_stop|red_is_dead",
        "keep_jumping|controls_reversed",
        "red_is_dead|controls_reversed",
        "floor_is_lava|controls_reversed",
        "red_is_dead|keep_jumping",
        "gravity_flip|keep_jumping"
    };

    [Header("Duration scaling")]
    [Range(0.4f, 1f)] public float durationScaleAtZeroIntensity = 0.7f;
    public float minRuleDuration = 8f;

    class Slot
    {
        public RuleBase Rule;
        public float WarnRemaining, Remaining, Total;
        public bool Active;
        public int ComboId;
        public bool Silent;
        public int DamageEventsAtStart;
    }

    readonly List<Slot> slots = new List<Slot>();
    readonly Dictionary<RuleBase, float> cooldownUntil = new Dictionary<RuleBase, float>();
    readonly List<RuleBase> candidates = new List<RuleBase>();
    readonly List<RuleBase> recent = new List<RuleBase>();
    RuleBase lastStarted;
    float nextRuleAt;
    float lastEndTime = -99f;
    int comboCounter;

    public event Action<RuleBase, float> RuleWarning;
    public event Action<RuleBase> RuleActivated;
    public event Action<RuleBase, bool> RuleEnded;
    public event Action RulesCleared;

    /// <summary>When true no NEW normal rule is scheduled (used during the Reality Break). Running rules are unaffected.</summary>
    public bool SchedulingPaused { get; set; }
    public bool HasPendingWarning => HasPending();
    public int SlotCount => slots.Count;
    public int ActiveCount { get { int n = 0; foreach (var s in slots) if (s.Active) n++; return n; } }
    public bool IsRuleQueuedOrActive(string id) { foreach (var s in slots) if (s.Rule.Id == id) return true; return false; }
    public float NextRuleAt => nextRuleAt;

    public bool TryGetSlot(int index, out RuleBase rule, out bool active, out float remaining, out float total)
    {
        rule = null; active = false; remaining = 0f; total = 0f;
        if (index < 0 || index >= slots.Count) return false;
        var s = slots[index];
        rule = s.Rule; active = s.Active; remaining = s.Active ? s.Remaining : s.WarnRemaining; total = s.Active ? s.Total : s.Rule.definition.warningDuration;
        return true;
    }

    void Awake()
    {
        if (rules == null || rules.Length == 0) { Debug.LogError("RuleManager: no rules assigned.", this); return; }
        foreach (var r in rules) r.Initialize();
        nextRuleAt = firstRuleTime;
    }

    // ------------------------------------------------------------------ intensity-driven parameters

    float CurrentIntensity(GameManager gm) => gm.intensity != null ? gm.intensity.Value : 0f;

    int PhaseIndex(GameManager gm) => gm.intensity != null ? (int)gm.intensity.Phase - 1 : 0;

    int MaxConcurrent(GameManager gm, float i) => PhaseIndex(gm) == 0 ? 1 : Mathf.Max(1, maxSimultaneousRules);

    float ComboChance(GameManager gm, float i)
    {
        int p = PhaseIndex(gm);
        return comboChanceByPhase != null && p >= 0 && p < comboChanceByPhase.Length ? comboChanceByPhase[p] : 0f;
    }

    /// <summary>Blends the interval bands so the cadence tightens gradually instead of stepping.</summary>
    float RollInterval(float i)
    {
        Vector2 r;
        if (i < 0.35f) r = earlyInterval;
        else if (i < 0.5f) r = Vector2.Lerp(earlyInterval, midInterval, Mathf.InverseLerp(0.35f, 0.5f, i));
        else if (i < 0.65f) r = midInterval;
        else if (i < 0.8f) r = Vector2.Lerp(midInterval, lateInterval, Mathf.InverseLerp(0.65f, 0.8f, i));
        else r = lateInterval;
        return UnityEngine.Random.Range(r.x, r.y);
    }

    float ScaledDuration(RuleDefinition d, float i) =>
        Mathf.Max(minRuleDuration, d.duration * Mathf.Lerp(durationScaleAtZeroIntensity, 1f, i));

    bool HasPending()
    {
        foreach (var s in slots) if (!s.Active) return true;
        return false;
    }

    // ------------------------------------------------------------------ update

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return; // nothing advances while paused / in menus / after death
        float dt = Time.deltaTime;
        float intensity = CurrentIntensity(gm);

        for (int i = slots.Count - 1; i >= 0; i--)
        {
            // A rule can kill the player during Tick, which resets (clears) the slots mid-iteration.
            if (gm.State != GameState.Playing || i >= slots.Count) return;
            var s = slots[i];
            if (!s.Active)
            {
                s.WarnRemaining -= dt;
                if (s.WarnRemaining > 0f) continue;
                s.Rule.Activate();
                s.Active = true;
                s.Total = s.Remaining = ScaledDuration(s.Rule.definition, intensity);
                s.DamageEventsAtStart = gm.health.DamageEvents;
                AudioManager.Play(Sfx.RuleStart);
                RuleActivated?.Invoke(s.Rule);
            }
            else
            {
                s.Rule.Tick(dt);
                if (gm.State != GameState.Playing) return; // the tick may have ended the run
                s.Remaining -= dt;
                if (s.Remaining > 0f) continue;
                FinishSlot(gm, i, s);
            }
        }

        float t = gm.SurvivalTime;
        if (t >= nextRuleAt && !HasPending() && !SchedulingPaused) Schedule(gm, t, intensity);
    }

    void FinishSlot(GameManager gm, int index, Slot s)
    {
        s.Rule.Deactivate();
        cooldownUntil[s.Rule] = gm.SurvivalTime + s.Rule.definition.cooldown;
        lastEndTime = gm.SurvivalTime;
        slots.RemoveAt(index);
        gm.RuleCompleted();
        AudioManager.Play(Sfx.RuleEnd);
        RuleEnded?.Invoke(s.Rule, true);

        if (gm.health.DamageEvents == s.DamageEventsAtStart) gm.RuleMastered(s.Rule); // no damage during this rule
        if (s.ComboId != 0 && !AnySlotInCombo(s.ComboId)) gm.ComboCompleted(2);       // both rules of the combo completed
    }

    bool AnySlotInCombo(int id)
    {
        foreach (var s in slots) if (s.ComboId == id) return true;
        return false;
    }

    void Schedule(GameManager gm, float t, float intensity)
    {
        int active = slots.Count;
        if (active == 0)
        {
            if (t - lastEndTime < minRestAfterRule) { nextRuleAt = t + 0.5f; return; }
            nextRuleAt = t + (TryStartRandom(t, intensity) ? RollInterval(intensity) : 2f);
            return;
        }

        float chance = ComboChance(gm, intensity);
        if (active >= MaxConcurrent(gm, intensity) || chance <= 0f)
        {
            nextRuleAt = t + 1f; // wait for a free slot; start promptly once one opens
            return;
        }
        bool started = UnityEngine.Random.value < chance && TryStartRandom(t, intensity);
        nextRuleAt = t + RollInterval(intensity) * (started ? 1f : 0.6f);
    }

    bool TryStartRandom(float t, float intensity)
    {
        candidates.Clear();
        float total = 0f;
        foreach (var r in rules)
        {
            if (r.definition.minSurvivalTime > t || IsQueued(r)) continue;
            if (cooldownUntil.TryGetValue(r, out var until) && until > t) continue;
            if (!CanRunTogether(r)) continue;
            candidates.Add(r);
        }
        if (candidates.Count > 1 && lastStarted != null) candidates.Remove(lastStarted);
        if (candidates.Count == 0) return false;

        foreach (var r in candidates) total += Weight(r, intensity);
        float pick = UnityEngine.Random.value * total;
        RuleBase chosen = candidates[candidates.Count - 1];
        foreach (var r in candidates)
        {
            pick -= Weight(r, intensity);
            if (pick <= 0f) { chosen = r; break; }
        }
        Queue(chosen);
        return true;
    }

    float Weight(RuleBase r, float intensity)
    {
        float w = 1f + intensity * 0.5f * (r.definition.difficulty - 1); // harder rules become likelier
        if (recent.Contains(r)) w *= 0.35f;                                // avoid repeating recent rules
        return w;
    }

    bool IsQueued(RuleBase r)
    {
        foreach (var s in slots) if (s.Rule == r) return true;
        return false;
    }

    /// <summary>A rule may start alone, or alongside others only if it is not incompatible AND the pair is on the combo allow-list.</summary>
    bool CanRunTogether(RuleBase r)
    {
        foreach (var s in slots)
            if (!r.IsCompatibleWith(s.Rule) || !ComboAllowed(r.Id, s.Rule.Id)) return false;
        return true;
    }

    bool ComboAllowed(string a, string b)
    {
        if (comboPairs == null) return false;
        foreach (var pair in comboPairs)
            if (pair == a + "|" + b || pair == b + "|" + a) return true;
        return false;
    }

    void Queue(RuleBase rule, bool silent = false, float warningOverride = 0f)
    {
        var slot = new Slot { Rule = rule, WarnRemaining = warningOverride > 0f ? warningOverride : rule.definition.warningDuration, Silent = silent };
        if (slots.Count > 0)
        {
            int id = ++comboCounter;
            slot.ComboId = id;
            foreach (var s in slots) s.ComboId = id;
        }
        slots.Add(slot);
        lastStarted = rule;
        recent.Add(rule);
        if (recent.Count > 2) recent.RemoveAt(0);
        rule.BeginWarning();
        if (!silent) AudioManager.Play(Sfx.Warning);
        RuleWarning?.Invoke(rule, slot.WarnRemaining);
    }

    /// <summary>Test / debug hook: queues a rule by id right now if it is allowed (same checks as the scheduler).</summary>
    public bool DebugStart(string id)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return false;
        foreach (var r in rules)
            if (r.Id == id && !IsQueued(r) && CanRunTogether(r) && !HasPending()) { Queue(r); return true; }
        return false;
    }

    /// <summary>True if the warning for this rule was started by a mutation (its own banner is suppressed).</summary>
    public bool IsSilentWarning(RuleBase rule)
    {
        foreach (var s in slots) if (s.Rule == rule) return s.Silent;
        return false;
    }

    /// <summary>A mutation's base rule is usable if it is already active, or could be started alongside the current rules.</summary>
    public bool IsEligibleBase(string id)
    {
        foreach (var r in rules)
        {
            if (r.Id != id) continue;
            if (IsRuleActive(id)) return true;
            return !IsQueued(r) && CanRunTogether(r);
        }
        return false;
    }

    /// <summary>Starts a rule for a mutation with a silent (no banner) warning of the given length. Returns true if the rule is active or queued.</summary>
    public bool QueueForMutation(string id, float warning)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return false;
        if (IsRuleActive(id)) return true;
        foreach (var r in rules)
        {
            if (r.Id != id) continue;
            if (IsQueued(r) || !CanRunTogether(r)) return IsQueued(r);
            Queue(r, true, warning);
            return true;
        }
        return false;
    }

    /// <summary>Makes sure an active rule lasts at least this many more seconds (so a mutation never outlives its base rule).</summary>
    public void EnsureRemaining(string id, float seconds)
    {
        foreach (var s in slots)
            if (s.Active && s.Rule.Id == id && s.Remaining < seconds) { s.Remaining = seconds; s.Total = Mathf.Max(s.Total, seconds); }
    }

    public bool IsRuleActive(string id)
    {
        foreach (var s in slots) if (s.Active && s.Rule.Id == id) return true;
        return false;
    }

    public void ResetAll()
    {
        foreach (var s in slots) s.Rule.Deactivate();
        slots.Clear();
        if (rules != null) foreach (var r in rules) r.Deactivate();
        cooldownUntil.Clear();
        recent.Clear();
        lastStarted = null;
        lastEndTime = -99f;
        comboCounter = 0;
        nextRuleAt = firstRuleTime;
        RulesCleared?.Invoke();
    }
}
