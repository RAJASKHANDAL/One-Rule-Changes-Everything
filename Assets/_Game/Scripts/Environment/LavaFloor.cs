using UnityEngine;

/// <summary>Visual overlay that turns the arena floor into lava. Damage is applied by FloorIsLavaRule.</summary>
public class LavaFloor : MonoBehaviour
{
    public Renderer overlay;
    public Material warningMaterial;
    public Material activeMaterial;

    Material warn, active;
    Color warnEmission, activeEmission;

    public HazardState State { get; private set; }

    void Awake()
    {
        warn = new Material(warningMaterial);
        active = new Material(activeMaterial);
        warnEmission = warningMaterial.GetColor("_EmissionColor");
        activeEmission = activeMaterial.GetColor("_EmissionColor");
        overlay.enabled = false;
    }

    public void SetState(HazardState state)
    {
        State = state;
        overlay.enabled = state != HazardState.Dormant;
        if (state == HazardState.Warning) overlay.sharedMaterial = warn;
        else if (state == HazardState.Active) overlay.sharedMaterial = active;
    }

    void Update()
    {
        if (State == HazardState.Warning)
            warn.SetColor("_EmissionColor", warnEmission * (0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * 6f))));
        else if (State == HazardState.Active)
            active.SetColor("_EmissionColor", activeEmission * (0.85f + 0.25f * Mathf.Sin(Time.time * 3f)));
    }
}
