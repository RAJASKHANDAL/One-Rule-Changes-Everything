using UnityEngine;

/// <summary>GRAVITY FLIP: the player's own gravity reverses at random intervals; the arena ceiling becomes a walkable surface.</summary>
public class GravityFlipRule : RuleBase
{
    public float minInterval = 3.5f;
    public float maxInterval = 5f;
    public float warningLead = 1.2f;

    const string Key = "gravity";
    float timer;
    bool warned;

    float NextInterval() => Random.Range(minInterval, maxInterval) * Mathf.Lerp(1.1f, 0.8f, Intensity);

    protected override void OnActivated()
    {
        timer = NextInterval();
        warned = false;
        Game.arena.SetCeilingVisible(true);
    }

    public override void Tick(float dt)
    {
        timer -= dt;
        if (!warned && timer <= warningLead)
        {
            warned = true;
            StatusBus.Set(Key, "GRAVITY FLIPPING!", definition.accent);
            AudioManager.Play(Sfx.Warning, 0.6f);
        }
        if (timer <= 0f)
        {
            var g = Game.gravity;
            g.SetInverted(this, !g.Inverted);
            AudioManager.Play(Sfx.Flip, 0.7f);
            Game.cameraRig.Shake(0.15f, 0.2f);
            timer = NextInterval();
            warned = false;
            StatusBus.Clear(Key);
        }
    }

    protected override void OnDeactivated()
    {
        Game.gravity.SetInverted(this, false);
        Game.arena.SetCeilingVisible(false);
        StatusBus.Clear(Key);
    }
}
