using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 3;
    public float invulnerableSeconds = 1f;

    float lastDamageTime = -99f;

    public int Current { get; private set; }
    public bool IsDead => Current <= 0;
    public bool IsLow => Current == 1;
    public bool IsInvulnerable => Time.time - lastDamageTime < invulnerableSeconds;
    /// <summary>Number of damage events taken this run (used for rule-mastery tracking).</summary>
    public int DamageEvents { get; private set; }

    public event Action<int, int> HealthChanged;
    public event Action<string> Damaged;

    void Awake() { Current = maxHealth; }

    /// <summary>Applies damage if the run is live and the damage cooldown has elapsed. Returns true if damage was applied.</summary>
    public bool TakeDamage(int amount, string source)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing || IsDead || IsInvulnerable) return false;

        lastDamageTime = Time.time;
        Current = Mathf.Max(0, Current - amount);
        DamageEvents++;
        gm.OnDamageTaken();
        HealthChanged?.Invoke(Current, maxHealth);
        Damaged?.Invoke(source);
        AudioManager.Play(Sfx.Hurt);
        gm.cameraRig.Shake(0.3f, 0.25f);
        if (Current == 1) AudioManager.Play(Sfx.LowHealth);
        if (Current == 0) gm.OnPlayerDied();
        return true;
    }

    public void ResetHealth()
    {
        Current = maxHealth;
        DamageEvents = 0;
        lastDamageTime = -99f;
        HealthChanged?.Invoke(Current, maxHealth);
    }
}
