using UnityEngine;

/// <summary>
/// Lets an existing safe platform slide between its original position and one validated destination.
/// Kinematic movement (MovePosition) plus passenger carrying, so a player standing on it is moved with it and never gets
/// a physics impulse. Always returns to its origin when released.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MovingPlatform : MonoBehaviour
{
    [Tooltip("World-space offset of the destination from the original position (validated by the builder).")]
    public Vector3 offset = new Vector3(2.6f, 0f, 0f);
    public float speed = 1.8f;
    public float dwell = 1.6f;
    [Tooltip("Optional flat marker shown at the destination while the move is announced.")]
    public GameObject destinationMarker;

    enum Mode { Idle, Cycling, Returning }

    Rigidbody rb;
    Collider solid;
    Vector3 origin;
    Mode mode;
    bool towardDestination = true;
    float dwellTimer;

    public bool IsIdle => mode == Mode.Idle;
    public Vector3 Destination => origin + offset;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        solid = GetComponent<Collider>();
        origin = transform.position;
        if (destinationMarker != null) destinationMarker.SetActive(false);
    }

    public bool PlayerOn()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.player.GroundCollider == null) return false;
        return gm.player.GroundCollider.attachedRigidbody == rb;
    }

    public void ShowMarker(bool visible) { if (destinationMarker != null) destinationMarker.SetActive(visible); }

    public void BeginCycle()
    {
        ShowMarker(false);
        mode = Mode.Cycling;
        towardDestination = true;
        dwellTimer = dwell * 0.5f;
    }

    public void BeginReturn() { if (mode != Mode.Idle) mode = Mode.Returning; }

    public void SnapToOrigin()
    {
        mode = Mode.Idle;
        towardDestination = true;
        ShowMarker(false);
        if (rb == null) return;
        rb.position = origin;
        transform.position = origin;
    }

    void FixedUpdate()
    {
        if (mode == Mode.Idle) return;
        float dt = Time.fixedDeltaTime;

        if (mode == Mode.Cycling)
        {
            if (dwellTimer > 0f) { dwellTimer -= dt; return; }
            var target = towardDestination ? origin + offset : origin;
            var next = Vector3.MoveTowards(rb.position, target, speed * dt);
            Move(next);
            if ((next - target).sqrMagnitude < 0.0001f) { towardDestination = !towardDestination; dwellTimer = dwell; }
        }
        else // Returning
        {
            var next = Vector3.MoveTowards(rb.position, origin, speed * 1.4f * dt);
            Move(next);
            if ((next - origin).sqrMagnitude < 0.0001f) mode = Mode.Idle;
        }
    }

    void Move(Vector3 next)
    {
        var delta = next - rb.position;
        bool carry = PlayerOn();
        rb.MovePosition(next);
        if (carry) GameManager.Instance.player.Body.position += delta; // passenger moves with the platform
    }
}
