using System;
using UnityEngine;

/// <summary>Coin combo: collect coins within a time window to build a multiplier that adds bonus points on top of the base coin value.</summary>
public class CoinComboManager : MonoBehaviour
{
    [Tooltip("Seconds allowed between coins to keep the combo alive.")]
    public float window = 3f;
    [Tooltip("Every N consecutive coins raises the multiplier by one.")]
    public int coinsPerMultiplierStep = 2;
    public int maxMultiplier = 5;
    [Tooltip("Combo counts that play a milestone sound.")]
    public int[] milestones = { 3, 5, 8, 12 };

    float timeLeft;

    public int Count { get; private set; }
    public int Multiplier { get; private set; } = 1;
    public int BestCombo { get; private set; }
    public float TimeLeft01 => window > 0f ? Mathf.Clamp01(timeLeft / window) : 0f;
    public bool Active => Count > 0;

    /// <summary>Raised when the combo changes: count, multiplier.</summary>
    public event Action<int, int> Changed;

    /// <summary>Registers a collected coin and returns the extra (bonus) points earned by the combo for this coin.</summary>
    public int RegisterCoin(int basePoints)
    {
        Count = timeLeft > 0f ? Count + 1 : 1;
        timeLeft = window;
        Multiplier = Mathf.Min(maxMultiplier, 1 + (Count - 1) / Mathf.Max(1, coinsPerMultiplierStep));
        BestCombo = Mathf.Max(BestCombo, Count);
        if (Array.IndexOf(milestones, Count) >= 0) AudioManager.Play(Sfx.Combo);
        Changed?.Invoke(Count, Multiplier);
        return basePoints * (Multiplier - 1);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing || timeLeft <= 0f) return;
        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f) EndCombo();
    }

    void EndCombo()
    {
        timeLeft = 0f; Count = 0; Multiplier = 1;
        Changed?.Invoke(0, 1);
    }

    public void ResetRun()
    {
        timeLeft = 0f; Count = 0; Multiplier = 1; BestCombo = 0;
        Changed?.Invoke(0, 1);
    }
}
