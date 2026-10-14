using UnityEngine;

/// <summary>Lightweight procedural animation for the primitive-built adventurer: idle, run, jump, land, hit, death, gravity roll.</summary>
public class PlayerVisual : MonoBehaviour
{
    public Transform legL, legR, armL, armR, scarfTail;
    public Renderer[] renderers;
    public ParticleSystem dust;
    [Header("Cosmetics (visual only)")]
    public Renderer[] scarfRenderers;
    public Renderer[] hoodRenderers;
    public TrailRenderer trail;
    public float baseScale = 1f;

    PlayerController pc;
    PlayerGravityController grav;
    PlayerHealth health;
    MaterialPropertyBlock block;
    Color[] baseColors;
    float walkPhase, yaw = 45f, roll, landSquash, hitTimer, deathT;
    bool dead, tinted;
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        pc = GetComponentInParent<PlayerController>();
        grav = GetComponentInParent<PlayerGravityController>();
        health = GetComponentInParent<PlayerHealth>();
        block = new MaterialPropertyBlock();
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].sharedMaterial.GetColor(BaseColorId);
        ApplyCosmetics();
    }

    /// <summary>Applies the equipped cosmetics. Colours and the trail only: no gameplay value is touched.</summary>
    public void ApplyCosmetics()
    {
        SetGroupColor(scarfRenderers, CosmeticManager.Equipped(CosmeticCategory.Scarf).Color);
        SetGroupColor(hoodRenderers, CosmeticManager.Equipped(CosmeticCategory.Hood).Color);
        var t = CosmeticManager.Equipped(CosmeticCategory.Trail);
        if (trail != null)
        {
            bool on = t.Color.a > 0f;
            trail.emitting = on && !dead;
            if (on) { trail.startColor = t.Color; trail.endColor = new Color(t.Color.r, t.Color.g, t.Color.b, 0f); }
            else trail.Clear();
        }
        ApplyTint(hitTimer > 0f ? 0.5f : 0f);
    }

    void SetGroupColor(Renderer[] group, Color color)
    {
        if (group == null) return;
        foreach (var g in group)
        {
            int idx = System.Array.IndexOf(renderers, g);
            if (idx >= 0) baseColors[idx] = color;
        }
    }

    void OnEnable()
    {
        pc.Landed += OnLanded;
        health.Damaged += OnDamaged;
        CosmeticManager.Changed += ApplyCosmetics;
    }

    void OnDisable()
    {
        if (pc != null) pc.Landed -= OnLanded;
        if (health != null) health.Damaged -= OnDamaged;
        CosmeticManager.Changed -= ApplyCosmetics;
    }

    void OnLanded(float impact)
    {
        if (impact <= 4f) return;
        landSquash = Mathf.Clamp01(impact / 16f) + 0.25f;
        if (dust != null && impact > 6f)
        {
            dust.transform.position = pc.transform.position + grav.Down * 0.95f;
            dust.Emit(Mathf.Clamp(Mathf.RoundToInt(impact), 5, 12));
        }
    }
    void OnDamaged(string source) { hitTimer = 0.6f; }

    public void PlayDeath() { dead = true; deathT = 0f; if (trail != null) trail.emitting = false; }

    public void ResetVisual()
    {
        dead = false; deathT = 0f; hitTimer = 0f; landSquash = 0f; roll = 0f;
        if (trail != null) trail.Clear();
        transform.localPosition = Vector3.zero;
        transform.localScale = Vector3.one * baseScale;
        ApplyCosmetics();
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        var f = pc.FacingDirection;
        yaw = Mathf.MoveTowardsAngle(yaw, Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg, 720f * dt);
        roll = Mathf.MoveTowards(roll, grav.Inverted ? 180f : 0f, 540f * dt);
        var rot = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(0f, 0f, roll);

        float speed01 = Mathf.Clamp01(pc.HorizontalSpeed / pc.moveSpeed);
        float t = Time.time;
        bool airborne = !pc.IsGrounded && !dead;

        float swing = 0f, armSwing = 0f, legSpread = 0f, armRaise = 0f;
        if (airborne)
        {
            legSpread = 22f; armRaise = -125f;
        }
        else
        {
            walkPhase += dt * (6f + speed01 * 8f) * (speed01 > 0.05f ? 1f : 0f);
            swing = Mathf.Sin(walkPhase) * 48f * speed01;
            armSwing = -swing;
        }
        legL.localRotation = Quaternion.Euler(swing - legSpread, 0f, 0f);
        legR.localRotation = Quaternion.Euler(-swing + legSpread, 0f, 0f);
        armL.localRotation = Quaternion.Euler(armSwing + armRaise, 0f, 0f);
        armR.localRotation = Quaternion.Euler(-armSwing + armRaise, 0f, 0f);
        scarfTail.localRotation = Quaternion.Euler(-8f + Mathf.Sin(t * 10f) * 8f - speed01 * 30f, 0f, 0f);

        // squash & stretch
        landSquash = Mathf.MoveTowards(landSquash, 0f, dt * 4f);
        float vy = Vector3.Dot(pc.Body.linearVelocity, grav.Up);
        float stretch = airborne ? Mathf.Clamp(Mathf.Abs(vy) / 12f, 0f, 1f) * 0.14f : 0f;
        float idle = Mathf.Sin(t * 2.2f) * 0.015f * (1f - speed01);
        var scale = new Vector3(1f - stretch * 0.5f + landSquash * 0.2f, 1f + stretch - landSquash * 0.25f + idle, 1f - stretch * 0.5f + landSquash * 0.2f);

        if (dead)
        {
            deathT = Mathf.Min(1f, deathT + dt * 2.2f);
            float e = 1f - (1f - deathT) * (1f - deathT);
            rot *= Quaternion.Euler(-90f * e, 0f, 0f);
            transform.localPosition = grav.Down * 0.65f * e;
            scale = Vector3.one;
        }
        else
        {
            transform.localPosition = Vector3.zero;
        }
        transform.rotation = rot;
        transform.localScale = scale * baseScale;

        if (hitTimer > 0f && !dead)
        {
            hitTimer -= dt;
            ApplyTint(Mathf.Sin(t * 60f) > 0f ? 0.75f : 0.2f);
        }
        else if (tinted) ApplyTint(0f);
    }

    void ApplyTint(float amount)
    {
        tinted = amount > 0f;
        for (int i = 0; i < renderers.Length; i++)
        {
            block.SetColor(BaseColorId, Color.Lerp(baseColors[i], new Color(1f, 0.15f, 0.2f), amount));
            renderers[i].SetPropertyBlock(block);
        }
    }
}
