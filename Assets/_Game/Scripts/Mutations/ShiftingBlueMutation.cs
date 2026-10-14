using System.Collections.Generic;
using UnityEngine;

/// <summary>RED IS DEAD -> SHIFTING BLUE: red tiles stay deadly while a few cyan platforms slide between predefined, validated spots.</summary>
public class ShiftingBlueMutation : MutationBase
{
    public MovingPlatform[] platforms;
    [Min(1)] public int movingCount = 2;
    [Min(1)] public int movingCountMastery = 3;

    readonly List<MovingPlatform> chosen = new List<MovingPlatform>();
    readonly List<MovingPlatform> pool = new List<MovingPlatform>();

    public override bool CanStart() => platforms != null && platforms.Length > 0;

    protected override void OnWarning()
    {
        // pick platforms that are idle and not under the player; announce their destinations with markers
        chosen.Clear(); pool.Clear();
        foreach (var p in platforms) if (p.IsIdle && !p.PlayerOn()) pool.Add(p);
        for (int i = pool.Count - 1; i > 0; i--) { int j = Random.Range(0, i + 1); (pool[i], pool[j]) = (pool[j], pool[i]); }
        int count = Game.mutations != null && Game.mutations.TierIndex >= 2 ? movingCountMastery : movingCount;
        for (int i = 0; i < pool.Count && chosen.Count < count; i++) chosen.Add(pool[i]);
        foreach (var p in chosen) p.ShowMarker(true);
    }

    protected override void OnWarningCancelled()
    {
        foreach (var p in chosen) p.ShowMarker(false);
        chosen.Clear();
    }

    protected override void OnActivated()
    {
        foreach (var p in chosen) p.BeginCycle();
    }

    protected override void OnDeactivated()
    {
        foreach (var p in chosen) p.BeginReturn(); // slide back home; passengers are carried
        chosen.Clear();
    }

    public override void ResetRun()
    {
        base.ResetRun();
        chosen.Clear();
        if (platforms != null) foreach (var p in platforms) p.SnapToOrigin();
    }
}
