using UnityEngine;

/// <summary>THE FLOOR IS LAVA: standing on the main floor hurts; raised safe platforms and jumping are safe.</summary>
public class FloorIsLavaRule : RuleBase
{
    public float burnDelay = 0.5f;
    public int damage = 1;

    const string Key = "lava";
    float onFloorTime, burn;
    bool warned;

    protected override void OnWarning() => Game.arena.SetLava(HazardState.Warning);
    protected override void OnWarningCancelled() => Game.arena.SetLava(HazardState.Dormant);

    protected override void OnActivated()
    {
        onFloorTime = 0f; warned = false;
        burn = burnDelay * Mathf.Lerp(1.2f, 0.8f, Intensity);
        Game.arena.SetLava(HazardState.Active);
    }

    public override void Tick(float dt)
    {
        if (!Game.player.IsOnFloorSurface)
        {
            onFloorTime = 0f;
            if (warned) { warned = false; StatusBus.Clear(Key); }
            return;
        }

        onFloorTime += dt;
        if (!warned && onFloorTime > burn * 0.4f)
        {
            warned = true;
            StatusBus.Set(Key, "GET OFF THE FLOOR!", definition.accent);
        }
        if (onFloorTime >= burn)
        {
            Game.health.TakeDamage(damage, "LAVA");
            onFloorTime = 0f;
        }
    }

    protected override void OnDeactivated()
    {
        onFloorTime = 0f; warned = false;
        Game.arena.SetLava(HazardState.Dormant);
        StatusBus.Clear(Key);
    }
}
