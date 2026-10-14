using UnityEngine;

public enum PlatformState { Safe, Warning, Gone, Recovering }

/// <summary>
/// A temporary platform: SAFE -> WARNING (blinks) -> GONE (collider off, hidden) -> RECOVERING (rises back) -> SAFE.
/// The collider is only re-enabled once the platform is fully back AND nothing is standing in its footprint,
/// so the player is never trapped inside it and never falls through a half-finished transition.
/// </summary>
public class PlatformTile : MonoBehaviour
{
    public Collider solid;
    public Transform visual;
    public Renderer topRenderer;
    public Material safeMaterial;
    public Material warningMaterial;
    [Tooltip("Extra margin around the platform that must be free of the player before it may return.")]
    public float returnClearance = 0.4f;

    float timer, goneDuration, recoverDuration;
    Vector3 fullScale;

    public PlatformState State { get; private set; }
    public bool IsSolid => State == PlatformState.Safe || State == PlatformState.Warning;

    void Awake()
    {
        fullScale = visual != null ? visual.localScale : Vector3.one;
    }

    /// <summary>True if the player's body overlaps this platform's footprint (used to avoid dropping / returning it onto them).</summary>
    public bool PlayerStandingOn()
    {
        var gm = GameManager.Instance;
        if (gm == null || solid == null) return false;
        var b = solid.bounds;
        b.Expand(new Vector3(returnClearance * 2f, 0f, returnClearance * 2f));
        var p = gm.player.transform.position;
        return p.x > b.min.x && p.x < b.max.x && p.z > b.min.z && p.z < b.max.z && p.y < b.max.y + 2.4f;
    }

    public bool BeginDisappear(float warning, float gone, float recover)
    {
        if (State != PlatformState.Safe) return false;
        goneDuration = gone; recoverDuration = recover;
        timer = warning;
        State = PlatformState.Warning;
        return true;
    }

    public void ResetNow()
    {
        State = PlatformState.Safe;
        timer = 0f;
        if (solid != null) solid.enabled = true;
        if (visual != null) { visual.gameObject.SetActive(true); visual.localScale = fullScale; }
        SetTop(safeMaterial);
    }

    void SetTop(Material m) { if (topRenderer != null && m != null && topRenderer.sharedMaterial != m) topRenderer.sharedMaterial = m; }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return; // frozen while paused / in menus / after death
        float dt = Time.deltaTime;

        switch (State)
        {
            case PlatformState.Warning:
                timer -= dt;
                SetTop(Mathf.FloorToInt(Time.time * 8f) % 2 == 0 ? warningMaterial : safeMaterial);
                if (visual != null) visual.localPosition = new Vector3(0f, 0.015f * Mathf.Sin(Time.time * 45f), 0f); // small tremble
                if (timer <= 0f)
                {
                    if (visual != null) visual.localPosition = Vector3.zero;
                    solid.enabled = false;
                    visual.gameObject.SetActive(false);
                    State = PlatformState.Gone;
                    timer = goneDuration;
                }
                break;

            case PlatformState.Gone:
                timer -= dt;
                if (timer <= 0f && !PlayerStandingOn())
                {
                    visual.gameObject.SetActive(true);
                    visual.localScale = new Vector3(fullScale.x, 0.01f, fullScale.z);
                    SetTop(warningMaterial);
                    State = PlatformState.Recovering;
                    timer = recoverDuration;
                }
                break;

            case PlatformState.Recovering:
                timer -= dt;
                float k = recoverDuration > 0f ? 1f - Mathf.Clamp01(timer / recoverDuration) : 1f;
                visual.localScale = new Vector3(fullScale.x, Mathf.Max(0.01f, fullScale.y * k), fullScale.z);
                if (timer <= 0f)
                {
                    if (PlayerStandingOn()) { timer = 0.25f; break; } // wait until clear, never trap the player
                    visual.localScale = fullScale;
                    solid.enabled = true;
                    SetTop(safeMaterial);
                    State = PlatformState.Safe;
                }
                break;
        }
    }
}
