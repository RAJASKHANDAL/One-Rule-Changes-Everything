using UnityEngine;

/// <summary>
/// NEVER STOP -> CHARGING ENERGY: standing still charges a visible energy meter. At full charge a telegraphed burst
/// hits once (then a cooldown). Moving drains the charge, and moving during the telegraph cancels the burst.
/// </summary>
public class ChargingEnergyMutation : MutationBase
{
    public float speedThreshold = 0.6f;
    [Tooltip("Seconds standing still before the meter starts charging.")]
    public float stationaryGrace = 0.7f;
    [Tooltip("Seconds of continuous charging to fill the meter.")]
    public float chargeSeconds = 4f;
    [Tooltip("Charge lost per second while moving.")]
    public float decayPerSecond = 0.8f;
    [Range(0.3f, 0.95f)] public float warnAt = 0.7f;
    public float burstTelegraph = 0.7f;
    public float burstCooldown = 3.5f;
    public int burstDamage = 1;

    const string Key = "energy";
    float stillTime, charge, telegraph, cooldown;
    bool warned;

    public float Charge01 => charge;
    public bool Telegraphing => telegraph > 0f;
    public bool MeterVisible => IsActive && (charge > 0.02f || cooldown > 0f);

    protected override void OnActivated() { ClearState(); }

    void ClearState() { stillTime = 0f; charge = 0f; telegraph = 0f; cooldown = 0f; warned = false; StatusBus.Clear(Key); }

    public override void Tick(float dt)
    {
        var p = Game.player;
        bool moving = p.HorizontalSpeed > speedThreshold || p.MoveInput.sqrMagnitude > 0.01f;

        if (cooldown > 0f) { cooldown -= dt; return; }

        if (telegraph > 0f)
        {
            if (moving) { telegraph = 0f; charge = 0.5f; StatusBus.Clear(Key); return; } // moved in time: burst fizzles
            telegraph -= dt;
            if (telegraph <= 0f) Burst();
            return;
        }

        if (moving)
        {
            stillTime = 0f;
            charge = Mathf.Max(0f, charge - decayPerSecond * dt);
            if (warned && charge < warnAt) { warned = false; StatusBus.Clear(Key); }
            return;
        }

        stillTime += dt;
        if (stillTime < stationaryGrace) return;
        charge = Mathf.Min(1f, charge + dt / chargeSeconds);
        if (!warned && charge >= warnAt)
        {
            warned = true;
            StatusBus.Set(Key, "ENERGY CRITICAL - MOVE!", accent);
            AudioManager.Play(Sfx.Warning, 0.7f);
        }
        if (charge >= 1f) { telegraph = burstTelegraph; AudioManager.Play(Sfx.Flip, 0.8f); }
    }

    void Burst()
    {
        // one telegraphed hit; the existing damage cooldown also protects against repeats
        Game.health.TakeDamage(burstDamage, "ENERGY BURST");
        Game.cameraRig.Shake(0.4f, 0.3f);
        if (Game.mutations != null && Game.mutations.visuals != null)
            Game.mutations.visuals.EmitBurst(Game.player.transform.position + Vector3.down * 0.8f, accent, 28);
        charge = 0f; stillTime = 0f; warned = false;
        cooldown = burstCooldown;
        StatusBus.Clear(Key);
    }

    protected override void OnDeactivated() { ClearState(); }
    public override void ResetRun() { base.ResetRun(); ClearState(); }
}
