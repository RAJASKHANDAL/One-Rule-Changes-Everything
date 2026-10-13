using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RED IS DEAD: a share of the designated hazard tiles (growing with intensity) lights up red and hurts the player.
/// A tile that would arm underneath the player keeps warning until the player steps off (or a short grace expires).
/// At higher intensity the active pattern periodically shifts, always with a warning blink first.
/// </summary>
public class RedIsDeadRule : RuleBase
{
    [Range(0.2f, 1f)] public float shareAtZeroIntensity = 0.45f;
    public float awayFromPlayerRadius = 2.4f;
    public float graceWhenPlayerOnTile = 1.2f;
    [Header("Pattern shifting (high intensity)")]
    public float shiftIntensityThreshold = 0.5f;
    public Vector2 shiftIntervalRange = new Vector2(5f, 8f);
    public float shiftWarning = 1.4f;

    readonly List<HazardTile> chosen = new List<HazardTile>();
    readonly List<HazardTile> incoming = new List<HazardTile>();
    readonly List<HazardTile> pool = new List<HazardTile>();
    float shiftTimer, incomingTimer;
    bool shifting;

    bool PlayerOn(HazardTile tile)
    {
        var p = Game.player.transform.position;
        var q = tile.transform.position;
        return Mathf.Abs(p.x - q.x) < 1.7f && Mathf.Abs(p.z - q.z) < 1.7f && p.y < 2.4f;
    }

    bool PlayerNear(HazardTile tile)
    {
        var p = Game.player.transform.position;
        var q = tile.transform.position;
        return new Vector2(p.x - q.x, p.z - q.z).magnitude < awayFromPlayerRadius;
    }

    /// <summary>Selects a subset of tiles sized by the current intensity, preferring tiles that are not next to the player.</summary>
    void Pick(List<HazardTile> into)
    {
        into.Clear();
        var all = Game.arena.hazardTiles;
        pool.Clear();
        pool.AddRange(all);
        for (int i = pool.Count - 1; i > 0; i--) // shuffle
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        int count = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(shareAtZeroIntensity, 1f, Intensity) * all.Length), 2, all.Length);
        foreach (var tile in pool) { if (into.Count >= count) break; if (!PlayerNear(tile)) into.Add(tile); }
        foreach (var tile in pool) { if (into.Count >= count) break; if (!into.Contains(tile)) into.Add(tile); }
    }

    protected override void OnWarning()
    {
        Pick(chosen);
        Game.arena.SetRedTiles(HazardState.Dormant);
        foreach (var tile in chosen) tile.SetState(HazardState.Warning);
    }

    protected override void OnWarningCancelled()
    {
        Game.arena.SetRedTiles(HazardState.Dormant);
        chosen.Clear();
    }

    protected override void OnActivated()
    {
        shifting = false;
        shiftTimer = NextShiftDelay();
        foreach (var tile in chosen) Arm(tile);
    }

    float NextShiftDelay() => Random.Range(shiftIntervalRange.x, shiftIntervalRange.y) * Mathf.Lerp(1.2f, 0.7f, Intensity);

    void Arm(HazardTile tile)
    {
        if (PlayerOn(tile)) { tile.SetState(HazardState.Warning); tile.armAfter = Time.time + graceWhenPlayerOnTile; }
        else tile.SetState(HazardState.Active);
    }

    public override void Tick(float dt)
    {
        // finish arming tiles that were held back because the player stood on them
        foreach (var tile in chosen)
            if (tile.State == HazardState.Warning && (!PlayerOn(tile) || Time.time >= tile.armAfter)) tile.SetState(HazardState.Active);

        if (Intensity < shiftIntensityThreshold) return;

        if (!shifting)
        {
            shiftTimer -= dt;
            if (shiftTimer > 0f) return;
            Pick(incoming);
            foreach (var tile in incoming) if (!chosen.Contains(tile)) tile.SetState(HazardState.Warning);
            shifting = true;
            incomingTimer = shiftWarning;
            return;
        }

        incomingTimer -= dt;
        if (incomingTimer > 0f) return;
        foreach (var tile in chosen) if (!incoming.Contains(tile)) tile.SetState(HazardState.Dormant);
        foreach (var tile in incoming) if (!chosen.Contains(tile)) Arm(tile);
        chosen.Clear();
        chosen.AddRange(incoming);
        shifting = false;
        shiftTimer = NextShiftDelay();
    }

    protected override void OnDeactivated()
    {
        Game.arena.SetRedTiles(HazardState.Dormant);
        chosen.Clear();
        incoming.Clear();
        shifting = false;
    }
}
