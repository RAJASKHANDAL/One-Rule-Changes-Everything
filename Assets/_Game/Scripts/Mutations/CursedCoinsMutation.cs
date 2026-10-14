using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// COINS ARE CURSED (mutation-only): each collected coin makes one nearby, currently safe floor tile warn and then turn
/// dangerous for a short time, after which it is restored. Each coin triggers at most one tile.
/// </summary>
public class CursedCoinsMutation : MutationBase
{
    public float warningSeconds = 1.6f;
    public float hazardSeconds = 3f;
    public float searchRadius = 9f;
    [Tooltip("Tiles closer than this to the player are never chosen (no unavoidable hits).")]
    public float minPlayerDistance = 2.8f;
    public int maxSimultaneous = 2;

    struct Cursed { public HazardTile Tile; public bool Active; public float Time; }
    readonly List<Cursed> cursed = new List<Cursed>();
    bool subscribed;

    protected override void OnActivated()
    {
        if (!subscribed && Game != null) { Game.CoinCollected += OnCoin; subscribed = true; }
    }

    void Unsubscribe()
    {
        if (subscribed && Game != null) Game.CoinCollected -= OnCoin;
        subscribed = false;
    }

    void OnCoin(Vector3 coinPosition)
    {
        if (!IsActive || Game.State != GameState.Playing || cursed.Count >= maxSimultaneous) return;
        var player = Game.player.transform.position;
        HazardTile best = null; float bestDist = float.MaxValue;
        foreach (var tile in Game.arena.hazardTiles)
        {
            if (tile.State != HazardState.Dormant) continue; // already owned by a rule / another effect
            var q = tile.transform.position;
            float toPlayer = new Vector2(player.x - q.x, player.z - q.z).magnitude;
            float toCoin = new Vector2(coinPosition.x - q.x, coinPosition.z - q.z).magnitude;
            if (toPlayer < minPlayerDistance || toCoin > searchRadius) continue;
            if (toCoin < bestDist) { bestDist = toCoin; best = tile; }
        }
        if (best == null) return; // no eligible tile: skip safely
        best.SetState(HazardState.Warning);
        cursed.Add(new Cursed { Tile = best, Active = false, Time = warningSeconds });
    }

    public override void Tick(float dt)
    {
        for (int i = cursed.Count - 1; i >= 0; i--)
        {
            var c = cursed[i];
            // if a rule took the tile over (e.g. RED IS DEAD reset it), stop managing it
            if (c.Tile.State != (c.Active ? HazardState.Active : HazardState.Warning)) { cursed.RemoveAt(i); continue; }
            c.Time -= dt;
            if (c.Time <= 0f)
            {
                if (!c.Active) { c.Tile.SetState(HazardState.Active); c.Active = true; c.Time = hazardSeconds; }
                else { c.Tile.SetState(HazardState.Dormant); cursed.RemoveAt(i); continue; }
            }
            cursed[i] = c;
        }
    }

    void Restore()
    {
        foreach (var c in cursed)
        {
            var expected = c.Active ? HazardState.Active : HazardState.Warning;
            if (c.Tile.State == expected) c.Tile.SetState(HazardState.Dormant);
        }
        cursed.Clear();
    }

    protected override void OnDeactivated() { Unsubscribe(); Restore(); }
    public override void ResetRun() { base.ResetRun(); Unsubscribe(); Restore(); }
    void OnDisable() { Unsubscribe(); }
}
