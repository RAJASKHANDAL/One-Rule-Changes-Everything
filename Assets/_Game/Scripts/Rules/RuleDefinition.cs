using UnityEngine;

/// <summary>Designer-facing configuration for a rule.</summary>
[CreateAssetMenu(menuName = "One Rule/Rule Definition", fileName = "Rule_")]
public class RuleDefinition : ScriptableObject
{
    public string id = "rule";
    public string displayName = "RULE";
    [Tooltip("Short instruction shown under the name in the announcement.")]
    public string subtitle = "DO SOMETHING";
    [TextArea] public string description;
    [Tooltip("Short glyph shown in the active-rules badge.")]
    public string glyph = "!";
    public Color accent = Color.cyan;

    public float duration = 15f;
    public float warningDuration = 2.5f;
    [Range(1, 3)] public int difficulty = 1;
    [Tooltip("Earliest survival time (seconds) at which this rule may be chosen.")]
    public float minSurvivalTime = 0f;
    [Tooltip("Seconds after the rule ends before it may be chosen again.")]
    public float cooldown = 30f;
    [Tooltip("Rules that must never be active at the same time as this one (ids).")]
    public string[] incompatibleWith;
}
