using UnityEngine;

/// <summary>
/// KEEP JUMPING -> LANDING SHOCKWAVE: every real landing sends a visible ring across the floor. The ring only hurts a player
/// who is standing on the ground when it passes, so jumping over it is the answer (which matches the base rule).
/// </summary>
public class LandingShockwaveMutation : MutationBase
{
    public GameObject ringPrefab;
    public float maxRadius = 6.5f;
    public float expansionSpeed = 7f;
    public float startRadius = 1.3f;
    [Tooltip("Minimum landing impact (fall speed) that creates a shockwave.")]
    public float minImpact = 3.5f;
    public float cooldown = 0.45f;
    public int damage = 1;
    public int poolSize = 3;

    struct Ring { public Transform T; public float Radius; public bool Hit; public bool Alive; public Vector3 Center; }
    Ring[] rings;
    float lastWave = -99f;
    bool subscribed;

    public override bool CanStart() => ringPrefab != null;

    void EnsurePool()
    {
        if (rings != null || ringPrefab == null) return;
        rings = new Ring[Mathf.Max(1, poolSize)];
        for (int i = 0; i < rings.Length; i++)
        {
            var go = Instantiate(ringPrefab, transform);
            go.name = "ShockwaveRing" + i;
            go.SetActive(false);
            rings[i].T = go.transform;
        }
    }

    protected override void OnActivated()
    {
        EnsurePool();
        if (!subscribed) { Game.player.Landed += OnLanded; subscribed = true; }
    }

    void Unsubscribe()
    {
        if (subscribed && Game != null && Game.player != null) Game.player.Landed -= OnLanded;
        subscribed = false;
    }

    void OnLanded(float impact)
    {
        if (!IsActive || impact < minImpact || Time.time - lastWave < cooldown || rings == null) return;
        for (int i = 0; i < rings.Length; i++)
        {
            if (rings[i].Alive) continue;
            lastWave = Time.time;
            var c = Game.player.transform.position;
            rings[i].Alive = true; rings[i].Hit = false; rings[i].Radius = startRadius;
            rings[i].Center = new Vector3(c.x, 0.07f, c.z);
            rings[i].T.position = rings[i].Center;
            rings[i].T.localScale = new Vector3(startRadius, 1f, startRadius);
            rings[i].T.gameObject.SetActive(true);
            return;
        }
    }

    public override void Tick(float dt)
    {
        if (rings == null) return;
        var p = Game.player;
        var pos = p.transform.position;
        for (int i = 0; i < rings.Length; i++)
        {
            if (!rings[i].Alive) continue;
            rings[i].Radius += expansionSpeed * dt;
            if (rings[i].Radius >= maxRadius) { Kill(i); continue; }
            rings[i].T.localScale = new Vector3(rings[i].Radius, 1f, rings[i].Radius);

            if (rings[i].Hit || !p.IsGrounded) continue; // airborne = jumped over it
            float d = new Vector2(pos.x - rings[i].Center.x, pos.z - rings[i].Center.z).magnitude;
            float band = 0.15f + 0.07f * rings[i].Radius;
            if (Mathf.Abs(d - rings[i].Radius * 0.93f) < band)
            {
                rings[i].Hit = true; // once per ring
                Game.health.TakeDamage(damage, "SHOCKWAVE");
            }
        }
    }

    void Kill(int i) { rings[i].Alive = false; rings[i].T.gameObject.SetActive(false); }

    void ClearRings()
    {
        if (rings == null) return;
        for (int i = 0; i < rings.Length; i++) Kill(i);
    }

    protected override void OnDeactivated() { Unsubscribe(); ClearRings(); }
    public override void ResetRun() { base.ResetRun(); Unsubscribe(); ClearRings(); lastWave = -99f; }
    void OnDisable() { Unsubscribe(); }
}
