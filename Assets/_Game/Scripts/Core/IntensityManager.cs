using UnityEngine;

/// <summary>
/// Single source of truth for how intense the current run is (0..1). Driven by survival time through a tunable curve
/// and smoothed so nothing ever jumps. Other systems (rules, hazards, audio, visuals, HUD) only read from here.
/// </summary>
public class IntensityManager : MonoBehaviour
{
    [Tooltip("X = survival time in seconds, Y = target intensity (0..1).")]
    public AnimationCurve curve = DefaultCurve();
    [Tooltip("Maximum change of the smoothed value per second.")]
    public float maxChangePerSecond = 0.04f;
    [Tooltip("Boundaries between the calm / medium / high tiers.")]
    public float mediumThreshold = 0.35f;
    public float highThreshold = 0.65f;

    public float Value { get; private set; }
    public float Peak { get; private set; }
    public bool IsMedium => Value >= mediumThreshold;
    public bool IsHigh => Value >= highThreshold;

    static AnimationCurve DefaultCurve()
    {
        float[] t = { 0f, 15f, 30f, 45f, 60f, 90f, 120f };
        float[] v = { 0.10f, 0.20f, 0.35f, 0.50f, 0.65f, 0.85f, 1.00f };
        var keys = new Keyframe[t.Length];
        for (int i = 0; i < t.Length; i++)
        {
            float inSlope = i > 0 ? (v[i] - v[i - 1]) / (t[i] - t[i - 1]) : 0f;
            float outSlope = i < t.Length - 1 ? (v[i + 1] - v[i]) / (t[i + 1] - t[i]) : 0f;
            keys[i] = new Keyframe(t[i], v[i], inSlope, outSlope);
        }
        return new AnimationCurve(keys);
    }

    void Awake() { ResetRun(); }

    public float Evaluate(float survivalTime)
    {
        if (curve == null || curve.length == 0) curve = DefaultCurve();
        return Mathf.Clamp01(curve.Evaluate(survivalTime));
    }

    public void ResetRun()
    {
        Value = Evaluate(0f);
        Peak = Value;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return; // frozen while paused, in menus and after death
        Value = Mathf.MoveTowards(Value, Evaluate(gm.SurvivalTime), maxChangePerSecond * Time.deltaTime);
        if (Value > Peak) Peak = Value;
    }
}
