using System;
using UnityEngine;

/// <summary>
/// Base class for gameplay rules. Lifecycle: Initialize -> (BeginWarning) -> Activate -> Tick... -> Deactivate.
/// Deactivate must restore every piece of shared state the rule changed.
/// </summary>
public abstract class RuleBase : MonoBehaviour
{
    public RuleDefinition definition;

    public bool IsActive { get; private set; }
    public bool IsWarning { get; private set; }
    public string Id => definition.id;
    protected GameManager Game => GameManager.Instance;
    /// <summary>Current run intensity (0..1); rules use it to tighten their timings.</summary>
    protected float Intensity => Game != null && Game.intensity != null ? Game.intensity.Value : 0f;

    public void Initialize()
    {
        if (definition == null) Debug.LogError($"{GetType().Name}: definition is not assigned.", this);
    }

    public void BeginWarning()
    {
        if (IsWarning || IsActive) return;
        IsWarning = true;
        OnWarning();
    }

    public void CancelWarning()
    {
        if (!IsWarning) return;
        IsWarning = false;
        OnWarningCancelled();
    }

    public void Activate()
    {
        if (IsActive) return;
        IsWarning = false;
        IsActive = true;
        OnActivated();
    }

    public void Deactivate()
    {
        if (IsWarning) CancelWarning();
        if (!IsActive) return;
        IsActive = false;
        OnDeactivated();
    }

    public virtual void Tick(float dt) { }
    protected virtual void OnWarning() { }
    protected virtual void OnWarningCancelled() { }
    protected abstract void OnActivated();
    protected abstract void OnDeactivated();

    public bool IsCompatibleWith(RuleBase other)
    {
        if (other == this) return false;
        return !Lists(definition.incompatibleWith, other.Id) && !Lists(other.definition.incompatibleWith, Id);
    }

    static bool Lists(string[] ids, string id) => ids != null && Array.IndexOf(ids, id) >= 0;
}
