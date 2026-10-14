using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Makes the arena react subtly to run intensity: crystals glow and pulse a little more, lighting gets slightly more
/// dramatic, the vignette tightens a touch, and rule activations spawn a small burst of particles.
/// Nothing is recoloured and every change is capped so hazards stay readable.
/// </summary>
public class IntensityVisuals : MonoBehaviour
{
    public Light sun;
    public Volume volume;
    public ParticleSystem ruleBurst;

    [Header("Ranges (calm -> maximum intensity)")]
    public Vector2 sunIntensity = new Vector2(1.25f, 1.42f);
    public Vector2 ambientIntensity = new Vector2(1f, 0.82f);
    public Vector2 vignetteIntensity = new Vector2(0.22f, 0.32f);
    public Vector2 crystalGlow = new Vector2(1f, 1.55f);
    public float crystalPulse = 0.12f;

    float boost;            // transient reality-break / mutation pulse (decays to 0)
    float mutationLook;     // 0..1 blend of the restrained magenta mutation tint
    bool mutationLookOn;
    Color baseAmbient;

    /// <summary>Brief crystal glow + light pulse (Reality Break, mutation activation).</summary>
    public void Pulse(float amount) { boost = Mathf.Max(boost, amount); }

    public void SetMutationLook(bool on) { mutationLookOn = on; }

    /// <summary>Small particle burst using the shared rule burst system.</summary>
    public void EmitBurst(Vector3 position, Color color, int count)
    {
        if (ruleBurst == null) return;
        ruleBurst.Emit(new ParticleSystem.EmitParams { position = position, startColor = color }, count);
    }

    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    GameManager gm;
    Vignette vignette;
    Renderer[] crystals;
    Color[] baseEmission;
    MaterialPropertyBlock block;
    float nextCrystalUpdate;
    float shown;

    void Start()
    {
        gm = GameManager.Instance;
        if (sun == null) sun = RenderSettings.sun;
        if (volume != null && volume.profile != null) volume.profile.TryGet(out vignette); // instance copy: the asset is never modified
        block = new MaterialPropertyBlock();
        baseAmbient = RenderSettings.ambientSkyColor;

        var list = new List<Renderer>();
        var colors = new List<Color>();
        foreach (var r in FindObjectsByType<MeshRenderer>())
        {
            if (!r.gameObject.name.StartsWith("Crystal") || r.sharedMaterial == null || !r.sharedMaterial.HasProperty(EmissionId)) continue;
            list.Add(r);
            colors.Add(r.sharedMaterial.GetColor(EmissionId));
        }
        crystals = list.ToArray();
        baseEmission = colors.ToArray();
    }

    void OnEnable()
    {
        var m = GameManager.Instance;
        if (m != null && m.rules != null) m.rules.RuleActivated += OnRuleActivated;
    }

    void OnDisable()
    {
        if (baseAmbient.a > 0f) RenderSettings.ambientSkyColor = baseAmbient;
        var m = GameManager.Instance;
        if (m != null && m.rules != null) m.rules.RuleActivated -= OnRuleActivated;
    }

    void OnRuleActivated(RuleBase rule)
    {
        if (ruleBurst == null || gm == null) return;
        float i = gm.intensity.Value;
        var emit = new ParticleSystem.EmitParams
        {
            position = gm.player.transform.position + Vector3.down * 0.8f,
            startColor = rule.definition.accent
        };
        ruleBurst.Emit(emit, Mathf.RoundToInt(Mathf.Lerp(10f, 26f, i)));
    }

    void Update()
    {
        if (gm == null) return;
        // while paused or after death keep the last look; in the menu stay calm
        float target = gm.State == GameState.MainMenu ? 0f : gm.intensity.Value;
        shown = Mathf.MoveTowards(shown, target, Time.unscaledDeltaTime * 0.2f);

        boost = Mathf.MoveTowards(boost, 0f, Time.unscaledDeltaTime * 0.9f);
        mutationLook = Mathf.MoveTowards(mutationLook, mutationLookOn ? 1f : 0f, Time.unscaledDeltaTime * 0.5f);
        RenderSettings.ambientSkyColor = Color.Lerp(baseAmbient, new Color(0.7f, 0.4f, 0.95f), mutationLook * 0.35f);
        if (sun != null) sun.intensity = Mathf.Lerp(sunIntensity.x, sunIntensity.y, shown) + boost * 0.35f;
        RenderSettings.ambientIntensity = Mathf.Lerp(ambientIntensity.x, ambientIntensity.y, shown);
        if (vignette != null) vignette.intensity.value = Mathf.Lerp(vignetteIntensity.x, vignetteIntensity.y, shown);

        if (crystals != null && Time.unscaledTime >= nextCrystalUpdate)
        {
            nextCrystalUpdate = Time.unscaledTime + 0.05f; // 20 Hz is plenty for a gentle glow
            float glow = Mathf.Lerp(crystalGlow.x, crystalGlow.y, shown) * (1f + boost * 1.4f);
            float pulse = 1f + crystalPulse * Mathf.Sin(Time.unscaledTime * Mathf.Lerp(1.5f, 3.5f, shown));
            for (int k = 0; k < crystals.Length; k++)
            {
                block.SetColor(EmissionId, baseEmission[k] * glow * pulse);
                crystals[k].SetPropertyBlock(block);
            }
        }
    }
}
