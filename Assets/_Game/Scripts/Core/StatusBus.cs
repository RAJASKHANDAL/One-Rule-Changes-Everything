using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Lightweight channel for short on-screen status lines ("KEEP MOVING!") that rules can raise and clear.</summary>
public static class StatusBus
{
    public struct Entry { public string Text; public Color Color; }

    public static event Action Changed;
    static readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
    static readonly List<string> order = new List<string>();

    public static IReadOnlyList<string> Keys => order;
    public static Entry Get(string key) => entries[key];

    public static void Set(string key, string text, Color color)
    {
        if (!entries.ContainsKey(key)) order.Add(key);
        entries[key] = new Entry { Text = text, Color = color };
        Changed?.Invoke();
    }

    public static void Clear(string key)
    {
        if (entries.Remove(key)) { order.Remove(key); Changed?.Invoke(); }
    }

    public static void ClearAll()
    {
        if (entries.Count == 0) return;
        entries.Clear(); order.Clear(); Changed?.Invoke();
    }
}
