using UnityEngine;

public enum HazardState { Dormant, Warning, Active }

/// <summary>A floor tile that is only dangerous while a rule switches it on. Keeps its original material and restores it afterwards.</summary>
public class HazardTile : MonoBehaviour
{
    public Renderer tileRenderer;
    public Material warningMaterial;
    public Material activeMaterial;
    [Tooltip("If true the tile is dangerous all the time and is never touched by rules.")]
    public bool permanent;
    public int damage = 1;
    /// <summary>Set by rules: a tile held in Warning because the player stood on it is armed at this time at the latest.</summary>
    [HideInInspector] public float armAfter;

    Material original, warn, active;
    Material shown;
    Color activeEmission;

    public HazardState State { get; private set; }
    public bool IsDangerous => permanent || State == HazardState.Active;

    void Awake()
    {
        original = tileRenderer.sharedMaterial;
        warn = new Material(warningMaterial);
        active = new Material(activeMaterial);
        activeEmission = activeMaterial.GetColor("_EmissionColor");
        shown = original;
    }

    public void SetState(HazardState state)
    {
        if (permanent) return;
        State = state;
        if (state == HazardState.Dormant) Show(original);
        else if (state == HazardState.Active) Show(active);
    }

    void Show(Material m)
    {
        if (shown == m) return;
        shown = m;
        tileRenderer.sharedMaterial = m;
    }

    void Update()
    {
        // pulses get faster and a little stronger as the run intensifies (never dazzling)
        var gm = GameManager.Instance;
        float i = gm != null && gm.intensity != null ? gm.intensity.Value : 0f;
        if (State == HazardState.Warning)
            Show(Mathf.FloorToInt(Time.time * Mathf.Lerp(6f, 10f, i)) % 2 == 0 ? warn : original);
        else if (State == HazardState.Active)
            active.SetColor("_EmissionColor", activeEmission * (0.85f + Mathf.Lerp(0.25f, 0.45f, i) * Mathf.Sin(Time.time * Mathf.Lerp(7f, 12f, i))));
    }

    void OnTriggerStay(Collider other)
    {
        if (!IsDangerous) return;
        var gm = GameManager.Instance;
        if (gm == null || other.attachedRigidbody != gm.player.Body) return;
        gm.health.TakeDamage(damage, "RED TILE");
    }
}
