using UnityEngine;

/// <summary>
/// Optional kinematic wooden beam that sweeps around a pivot at a constant, predictable speed. It lives at the arena edge,
/// is only enabled by ArenaDynamics from a configurable phase, and is moved with MovePosition/MoveRotation so physics stays stable.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MovingObstacle : MonoBehaviour
{
    public float degreesPerSecond = 28f;
    Rigidbody rb;
    float angle;
    Quaternion baseRotation;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        baseRotation = transform.rotation;
    }

    void OnEnable() { angle = 0f; if (rb != null) rb.rotation = baseRotation; }

    void FixedUpdate()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;
        angle += degreesPerSecond * Time.fixedDeltaTime;
        rb.MoveRotation(baseRotation * Quaternion.Euler(0f, angle, 0f));
    }
}
