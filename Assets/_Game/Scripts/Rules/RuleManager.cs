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
    [Range(0f, 1f)] public float comboChanceMid = 0.35f;
    [Range(0f, 1f)] public float comboChanceLate = 0.75f;
    [Tooltip("Pairs (ruleIdA|ruleIdB) that were checked against the implementations and may run together.")]
    public string[] comboPairs =
    {
        "never_stop|red_is_dead",
        "keep_jumping|controls_reversed",
        "red_is_dead|controls_reversed",
        "floor_is_lava|controls_reversed"
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

    public int SlotCount => slots.Count;
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

    int MaxConcurrent(GameManager gm, float i)
    {
        var im = gm.intensity;
        float med = im != null ? im.mediumThreshold : 0.35f, high = im != null ? im.highThreshold : 0.65f;
        return i < med ? maxConcurrentEarly : i < high ? maxConcurrentMid : maxConcurrentLate;
    }

    float ComboChance(GameManager gm, float i)
    {
        var im = gm.intensity;
        float med = im != null ? im.mediumThreshold : 0.35f, high = im != null ? im.highThreshold : 0.65f;
        if (i < med) return 0f;
        return i < high ? comboChanceMid : comboChanceLate;
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
        if (t >= nextRuleAt && !HasPending()) Schedule(gm, t, intensity);
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

    void Queue(RuleBase rule)
    {
        var slot = new Slot { Rule = rule, WarnRemaining = rule.definition.warningDuration };
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
        AudioManager.Play(Sfx.Warning);
        RuleWarning?.Invoke(rule, rule.definition.warningDuration);
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
