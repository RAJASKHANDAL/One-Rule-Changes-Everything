using UnityEngine;

/// <summary>KEEP JUMPING: staying on the ground longer than the grace period costs one health point.</summary>
public class KeepJumpingRule : RuleBase
{
    public float gracePeriod = 1.1f;
    public float warningFraction = 0.55f;
    public float repeatDelay = 0.5f;
    public int damage = 1;

    const string Key = "keepjumping";
    float groundedTime, grace;
    bool warned;

    protected override void OnActivated()
    {
        groundedTime = 0f; warned = false;
        grace = gracePeriod * Mathf.Lerp(1.15f, 0.85f, Intensity);
    }

    public override void Tick(float dt)
    {
        if (!Game.player.IsGrounded)
        {
            groundedTime = 0f;
            if (warned) { warned = false; StatusBus.Clear(Key); }
            return;
        }

        groundedTime += dt;
        if (!warned && groundedTime >= grace * warningFraction)
        {
            warned = true;
            StatusBus.Set(Key, "JUMP!", definition.accent);
            AudioManager.Play(Sfx.Warning, 0.6f);
        }
        if (groundedTime >= grace)
        {
            Game.health.TakeDamage(damage, "KEEP JUMPING");
            groundedTime = -repeatDelay;
        }
    }

    protected override void OnDeactivated()
    {
        groundedTime = 0f; warned = false;
        StatusBus.Clear(Key);
    }
}
