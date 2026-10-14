using System;
using System.Collections.Generic;
using UnityEngine;

public enum MissionType { SurviveSeconds, CollectCoins, RulesWithoutDamage, CoinCombo, ReachScore, TwoRulesSeconds, NearMisses }

[Serializable]
public class Mission
{
    public MissionType Type;
    public int Target;
    public int Progress;
    public bool Completed;
    public int RewardScore;
    public int RewardCoins;

    public string Description
    {
        get
        {
            switch (Type)
            {
                case MissionType.SurviveSeconds: return "SURVIVE " + Target + "s";
                case MissionType.CollectCoins: return "COLLECT " + Target + " COINS";
                case MissionType.RulesWithoutDamage: return Target + " RULES, NO DAMAGE";
                case MissionType.CoinCombo: return "COIN COMBO x" + Target;
                case MissionType.ReachScore: return "SCORE " + Target;
                case MissionType.TwoRulesSeconds: return Target + "s WITH 2 RULES";
                default: return Target + " NEAR MISSES";
            }
        }
    }
}

/// <summary>Three lightweight run missions (easy / medium / hard) tracked from gameplay state; each completes and pays out exactly once.</summary>
public class MissionManager : MonoBehaviour
{
    public int missionCount = 3;
    [Header("Targets per tier (easy, medium, hard)")]
    public int[] surviveSeconds = { 30, 60, 90 };
    public int[] coins = { 10, 18, 28 };
    public int[] rulesWithoutDamage = { 2, 3, 4 };
    public int[] combo = { 3, 5, 8 };
    public int[] score = { 500, 1200, 2400 };
    public int[] twoRuleSeconds = { 8, 15, 25 };
    public int[] nearMisses = { 1, 2, 4 };
    [Header("Rewards per tier")]
    public int[] rewardScore = { 75, 150, 300 };
    public int[] rewardCoins = { 5, 10, 20 };

    readonly List<Mission> missions = new List<Mission>();
    float twoRuleTime;

    public IReadOnlyList<Mission> Missions => missions;
    public int CompletedCount { get; private set; }

    /// <summary>Raised when the list or progress changed (HUD refresh).</summary>
    public event Action Changed;
    public event Action<Mission> MissionCompleted;

    public void ResetRun()
    {
        missions.Clear();
        CompletedCount = 0; twoRuleTime = 0f;
        var types = (MissionType[])Enum.GetValues(typeof(MissionType));
        var pool = new List<MissionType>(types);
        for (int tier = 0; tier < missionCount && pool.Count > 0; tier++)
        {
            int pick = UnityEngine.Random.Range(0, pool.Count);
            var type = pool[pick];
            pool.RemoveAt(pick);
            int t = Mathf.Min(tier, 2);
            missions.Add(new Mission { Type = type, Target = TargetFor(type, t), RewardScore = rewardScore[t], RewardCoins = rewardCoins[t] });
        }
        Changed?.Invoke();
    }

    int TargetFor(MissionType type, int tier)
    {
        switch (type)
        {
            case MissionType.SurviveSeconds: return surviveSeconds[tier];
            case MissionType.CollectCoins: return coins[tier];
            case MissionType.RulesWithoutDamage: return rulesWithoutDamage[tier];
            case MissionType.CoinCombo: return combo[tier];
            case MissionType.ReachScore: return score[tier];
            case MissionType.TwoRulesSeconds: return twoRuleSeconds[tier];
            default: return nearMisses[tier];
        }
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;
        if (gm.rules.ActiveCount >= 2) twoRuleTime += Time.deltaTime;

        bool changed = false;
        foreach (var m in missions)
        {
            if (m.Completed) continue;
            int value = ValueOf(m.Type, gm);
            int progress = Mathf.Min(value, m.Target);
            if (progress != m.Progress) { m.Progress = progress; changed = true; }
            if (value >= m.Target)
            {
                m.Completed = true; // flag first: a mission can only ever complete once
                CompletedCount++;
                changed = true;
                gm.OnMissionCompleted(m);
                AudioManager.Play(Sfx.MissionDone);
                MissionCompleted?.Invoke(m);
            }
        }
        if (changed) Changed?.Invoke();
    }

    int ValueOf(MissionType type, GameManager gm)
    {
        switch (type)
        {
            case MissionType.SurviveSeconds: return Mathf.FloorToInt(gm.SurvivalTime);
            case MissionType.CollectCoins: return gm.Coins;
            case MissionType.RulesWithoutDamage: return gm.RulesSinceDamage;
            case MissionType.CoinCombo: return gm.combo != null ? gm.combo.BestCombo : 0;
            case MissionType.ReachScore: return gm.Score;
            case MissionType.TwoRulesSeconds: return Mathf.FloorToInt(twoRuleTime);
            default: return gm.nearMiss != null ? gm.nearMiss.Count : 0;
        }
    }
}
