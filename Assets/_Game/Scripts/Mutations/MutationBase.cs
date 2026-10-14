using UnityEngine;

public enum MutationId { ShiftingBlue, ChargingEnergy, CursedCoins, LandingShockwave }

/// <summary>
/// A mutation modifies an existing normal rule (its base rule keeps running). Lifecycle:
/// eligibility (CanStart) -> BeginWarning -> Activate -> Tick... -> Deactivate. Deactivate must leave no side effects behind;
/// ResetRun is the hard clean-up used on restart / game over.
/// </summary>
public abstract class MutationBase : MonoBehaviour
{
    public MutationId id;
    [Tooltip("RuleDefinition id of the normal rule this mutation modifies. Empty = mutation-only.")]
    public string baseRuleId = "";
    public string displayName = "MUTATION";
    [Tooltip("Shown in the warning banner, e.g. RED IS DEAD -> SHIFTING BLUE")]
    public string headline = "";
    [Tooltip("What changes / what to do differently.")]
    public string description = "";
    public float duration = 16f;
    public float weight = 1f;
    [Range(0, 2), Tooltip("0 = available from the introduction, 1 = from adaptation, 2 = from mastery.")]
    public int minTier = 0;
    public Color accent = new Color(0.82f, 0.4f, 1f);

    public bool IsWarning { get; private set; }
    public bool IsActive { get; private set; }
    [System.NonSerialized] public float Remaining;
    [System.NonSerialized] public float WarnRemaining;
    [System.NonSerialized] public int DamageAtStart;

    protected GameManager Game => GameManager.Instance;

    public virtual bool CanStart() => true;

    public void BeginWarning() { if (IsWarning || IsActive) return; IsWarning = true; OnWarning(); }
    public void CancelWarning() { if (!IsWarning) return; IsWarning = false; OnWarningCancelled(); }
    public void Activate() { if (IsActive) return; IsWarning = false; IsActive = true; OnActivated(); }
    public void Deactivate()
    {
        if (IsWarning) CancelWarning();
        if (!IsActive) return;
        IsActive = false;
        OnDeactivated();
    }

    public virtual void Tick(float dt) { }
    /// <summary>Hard reset: deactivate and clear any leftover state immediately.</summary>
    public virtual void ResetRun() { Deactivate(); }

    protected virtual void OnWarning() { }
    protected virtual void OnWarningCancelled() { }
    protected abstract void OnActivated();
    protected abstract void OnDeactivated();

    protected virtual void OnValidate()
    {
        duration = Mathf.Max(3f, duration);
        weight = Mathf.Max(0f, weight);
    }
}
