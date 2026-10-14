using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes the arena change over a run. Phase 2+: single warning-based hazard flashes on red tiles.
/// Phase 3+: temporary platforms disappear (after a warning) and recover. Phase 3+: optional sweeping obstacle.
/// Never touches the nine permanent safe platforms, so a route always exists, and never acts on tiles while the
/// RED IS DEAD rule owns them.
/// </summary>
public class ArenaDynamics : MonoBehaviour
{
    public PlatformTile[] temporaryPlatforms;
    public MovingObstacle[] obstacles;

    [Header("Dynamic hazard flashes")]
    public bool enableHazardFlashes = true;
    public Vector2 hazardInterval = new Vector2(9f, 14f);
    public float hazardWarning = 1.8f;
    public float hazardActive = 2.5f;
    [Tooltip("Hazards never start closer than this to the player.")]
    public float hazardMinPlayerDistance = 3.2f;

    [Header("Temporary platforms")]
    public bool enableDisappearingPlatforms = true;
    public Vector2 platformInterval = new Vector2(7f, 12f);
    public float platformWarning = 2.2f;
    public float platformMinWarning = 1.6f;
    public float platformGone = 3.5f;
    public float platformRecover = 1.0f;
    public int maxPlatformsGoneAdaptation = 1;
    public int maxPlatformsGoneLater = 2;

    [Header("Moving obstacles (optional)")]
    public bool enableMovingObstacles = true;
    public RunPhase obstaclesFromPhase = RunPhase.Adaptation;

    struct FlashingTile { public HazardTile Tile; public bool Active; public float Time; }

    readonly List<FlashingTile> flashes = new List<FlashingTile>();
    readonly List<HazardTile> candidates = new List<HazardTile>();
    float hazardTimer, platformTimer;
    GameManager gm;

    void Start()
    {
        gm = GameManager.Instance;
        ResetRun();
    }

    void OnEnable()
    {
        var m = GameManager.Instance;
        if (m == null) return;
        m.RunReset += ResetRun;
        if (m.rules != null) m.rules.RuleWarning += OnRuleWarning;
    }

    void OnDisable()
    {
        var m = GameManager.Instance;
        if (m == null) return;
        m.RunReset -= ResetRun;
        if (m.rules != null) m.rules.RuleWarning -= OnRuleWarning;
    }

    /// <summary>When RED IS DEAD starts warning it takes over the tiles: release anything we had flashing.</summary>
    void OnRuleWarning(RuleBase rule, float duration)
    {
        if (rule.Id != "red_is_dead") return;
        ReleaseFlashes();
    }

    void ReleaseFlashes()
    {
        foreach (var f in flashes) f.Tile.SetState(HazardState.Dormant);
        flashes.Clear();
    }

    public void ResetRun()
    {
        ReleaseFlashes();
        foreach (var p in temporaryPlatforms) p.ResetNow();
        foreach (var o in obstacles) o.gameObject.SetActive(false);
        hazardTimer = Random.Range(hazardInterval.x, hazardInterval.y);
        platformTimer = Random.Range(platformInterval.x, platformInterval.y);
    }

    void Update()
    {
        if (gm == null || gm.State != GameState.Playing) return;
        float dt = Time.deltaTime;
        var phase = gm.intensity.Phase;
        float intensity = gm.intensity.Value;

        UpdateObstacles(phase);
        UpdateFlashes(dt, phase, intensity);
        UpdatePlatforms(dt, phase, intensity);
    }

    void UpdateObstacles(RunPhase phase)
    {
        bool on = enableMovingObstacles && phase >= obstaclesFromPhase;
        foreach (var o in obstacles) if (o.gameObject.activeSelf != on) o.gameObject.SetActive(on);
    }

    bool RedRuleOwnsTiles => gm.rules.IsRuleQueuedOrActive("red_is_dead");

    void UpdateFlashes(float dt, RunPhase phase, float intensity)
    {
        // progress existing flashes: warning -> active -> dormant
        for (int i = flashes.Count - 1; i >= 0; i--)
        {
            var f = flashes[i];
            f.Time -= dt;
            if (f.Time <= 0f)
            {
                if (!f.Active) { f.Tile.SetState(HazardState.Active); f.Active = true; f.Time = hazardActive; }
                else { f.Tile.SetState(HazardState.Dormant); flashes.RemoveAt(i); continue; }
            }
            flashes[i] = f;
        }

        if (!enableHazardFlashes || phase < RunPhase.FirstChanges || RedRuleOwnsTiles) return;
        hazardTimer -= dt;
        if (hazardTimer > 0f) return;
        hazardTimer = Random.Range(hazardInterval.x, hazardInterval.y) * Mathf.Lerp(1f, 0.6f, intensity);

        int maxFlashes = phase >= RunPhase.HighPressure ? 2 : 1;
        if (flashes.Count >= maxFlashes) return;

        // pick a dormant tile that is not near the player
        candidates.Clear();
        var p = gm.player.transform.position;
        foreach (var tile in gm.arena.hazardTiles)
        {
            if (tile.State != HazardState.Dormant) continue;
            var q = tile.transform.position;
            if (new Vector2(p.x - q.x, p.z - q.z).magnitude < hazardMinPlayerDistance) continue;
            candidates.Add(tile);
        }
        if (candidates.Count == 0) return;
        var chosen = candidates[Random.Range(0, candidates.Count)];
        chosen.SetState(HazardState.Warning);
        flashes.Add(new FlashingTile { Tile = chosen, Active = false, Time = hazardWarning });
    }

    void UpdatePlatforms(float dt, RunPhase phase, float intensity)
    {
        if (!enableDisappearingPlatforms || phase < RunPhase.Adaptation) return;
        platformTimer -= dt;
        if (platformTimer > 0f) return;
        platformTimer = Random.Range(platformInterval.x, platformInterval.y) * Mathf.Lerp(1f, 0.65f, intensity);

        int maxGone = phase >= RunPhase.HighPressure ? maxPlatformsGoneLater : maxPlatformsGoneAdaptation;
        int unavailable = 0;
        foreach (var plat in temporaryPlatforms) if (plat.State != PlatformState.Safe) unavailable++;
        if (unavailable >= maxGone) return;

        // prefer a platform nobody is standing on; otherwise allow it but only with the full (never shortened) warning
        PlatformTile choice = null;
        var spares = new List<PlatformTile>(temporaryPlatforms.Length);
        foreach (var plat in temporaryPlatforms) if (plat.State == PlatformState.Safe) spares.Add(plat);
        for (int tries = 0; tries < spares.Count && choice == null; tries++)
        {
            var c = spares[Random.Range(0, spares.Count)];
            if (!c.PlayerStandingOn()) choice = c;
        }
        float warning = Mathf.Max(platformMinWarning, Mathf.Lerp(platformWarning, platformMinWarning, intensity));
        if (choice == null && spares.Count > 0) { choice = spares[Random.Range(0, spares.Count)]; warning = Mathf.Max(warning, platformWarning); }
        if (choice != null) choice.BeginDisappear(warning, platformGone, platformRecover);
    }
}
