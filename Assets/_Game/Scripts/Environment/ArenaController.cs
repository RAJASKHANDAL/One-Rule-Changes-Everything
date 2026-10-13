using UnityEngine;

/// <summary>Single access point for rules that modify the arena (hazard tiles, lava floor, ceiling visibility).</summary>
public class ArenaController : MonoBehaviour
{
    public HazardTile[] hazardTiles;
    public LavaFloor lava;
    public GameObject ceilingVisual;

    void Awake()
    {
        if (hazardTiles == null || lava == null)
            Debug.LogError("ArenaController: hazard tiles or lava floor not assigned.", this);
    }

    public void SetRedTiles(HazardState state)
    {
        foreach (var tile in hazardTiles) tile.SetState(state);
    }

    public void SetLava(HazardState state) => lava.SetState(state);

    public void SetCeilingVisible(bool visible)
    {
        if (ceilingVisual != null) ceilingVisual.SetActive(visible);
    }

    public void ResetArena()
    {
        SetRedTiles(HazardState.Dormant);
        SetLava(HazardState.Dormant);
        SetCeilingVisible(false);
    }
}
