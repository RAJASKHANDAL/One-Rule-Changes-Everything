using UnityEngine;

/// <summary>NEVER STOP: standing still too long costs one health point (then a short penalty delay before it can repeat).</summary>
public class NeverStopRule : RuleBase
{
    public float speedThreshold = 0.6f;
    public float gracePeriod = 0.8f;
    public float warningLead = 0.4f;
    public float repeatDelay = 0.6f;
    public int damage = 1;

    const string Key = "neverstop";
    float stillTime, grace;
    bool warned;

    protected override void OnActivated()
    {
        stillTime = 0f; warned = false;
        grace = gracePeriod * Mathf.Lerp(1.15f, 0.8f, Intensity); // tighter as the run intensifies
    }

    public override void Tick(float dt)
    {
        var p = Game.player;
        bool moving = p.HorizontalSpeed > speedThreshold || p.MoveInput.sqrMagnitude > 0.01f;
        if (moving)
        {
            stillTime = 0f;
            if (warned) { warned = false; StatusBus.Clear(Key); }
            return;
        }

        stillTime += dt;
        if (!warned && stillTime >= grace - warningLead)
        {
            warned = true;
            StatusBus.Set(Key, "KEEP MOVING!", definition.accent);
            AudioManager.Play(Sfx.Warning, 0.6f);
        }
        if (stillTime >= grace)
        {
            Game.health.TakeDamage(damage, "NEVER STOP");
            stillTime = -repeatDelay; // one hit per violation, not per frame
        }
    }

    protected override void OnDeactivated()
    {
        stillTime = 0f; warned = false;
        StatusBus.Clear(Key);
    }
}
