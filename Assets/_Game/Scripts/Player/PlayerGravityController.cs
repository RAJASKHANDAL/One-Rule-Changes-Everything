using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Per-player gravity. Rules request inversion through owners, so overlapping requests never fight each other.</summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerGravityController : MonoBehaviour
{
    public float gravity = 30f;
    public float fallMultiplier = 1.3f;
    public float maxFallSpeed = 28f;

    Rigidbody rb;
    readonly HashSet<object> inverters = new HashSet<object>();

    public bool Inverted => inverters.Count > 0;
    public Vector3 Down => Inverted ? Vector3.up : Vector3.down;
    public Vector3 Up => -Down;
    public event Action<bool> InvertedChanged;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    public void SetInverted(object owner, bool inverted)
    {
        bool before = Inverted;
        if (inverted) inverters.Add(owner); else inverters.Remove(owner);
        if (before != Inverted) OnChanged();
    }

    public void ResetGravity()
    {
        bool before = Inverted;
        inverters.Clear();
        if (before) OnChanged();
    }

    void OnChanged()
    {
        var v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;
        InvertedChanged?.Invoke(Inverted);
    }

    void FixedUpdate()
    {
        var down = Down;
        var v = rb.linearVelocity;
        float fall = Vector3.Dot(v, down);
        rb.AddForce(down * gravity * (fall > 0f ? fallMultiplier : 1f), ForceMode.Acceleration);
        if (fall > maxFallSpeed) rb.linearVelocity = v - down * (fall - maxFallSpeed);
    }
}
