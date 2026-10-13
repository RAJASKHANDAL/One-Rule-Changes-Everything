using System.Collections.Generic;
using UnityEngine;

/// <summary>Central place where rules alter player input. Owners are tracked so a rule can never stack inversions.</summary>
public static class InputModifier
{
    static readonly HashSet<object> reverseOwners = new HashSet<object>();

    public static bool Reversed => reverseOwners.Count > 0;
    public static void AddReverse(object owner) => reverseOwners.Add(owner);
    public static void RemoveReverse(object owner) => reverseOwners.Remove(owner);
    public static void Clear() => reverseOwners.Clear();
    public static Vector2 Apply(Vector2 move) => Reversed ? -move : move;
}
