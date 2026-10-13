using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Rigidbody character controller: camera-relative WASD, jump with coyote time + buffering, gravity-direction aware ground check.</summary>
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(PlayerGravityController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpHeight = 2f;
    public float groundAcceleration = 80f;
    public float airAcceleration = 40f;
    public float coyoteTime = 0.1f;
    public float jumpBuffer = 0.12f;
    public float groundCheckExtra = 0.12f;

    [Header("References")]
    public CameraRig cameraRig;
    public Vector3 spawnPosition = new Vector3(0f, 0.85f, 0f);

    [Header("Automated testing hooks")]
    public Vector2 debugMove;
    public bool debugJump;

    Rigidbody rb;
    CapsuleCollider capsule;
    PlayerGravityController grav;
    float jumpBufferTimer, coyoteTimer;
    Vector2 rawInput;
    bool onFloorSurface;

    public bool InputEnabled { get; set; }
    public bool IsGrounded { get; private set; }
    public Collider GroundCollider { get; private set; }
    public bool IsOnFloorSurface => IsGrounded && onFloorSurface;
    public Vector2 MoveInput { get; private set; }
    public float HorizontalSpeed { get; private set; }
    public Vector3 FacingDirection { get; private set; } = new Vector3(1f, 0f, 1f).normalized;
    public Rigidbody Body => rb;

    public event Action Jumped;
    public event Action<float> Landed;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        grav = GetComponent<PlayerGravityController>();
        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        if (cameraRig == null) Debug.LogError("PlayerController: cameraRig is not assigned.", this);
    }

    void Update()
    {
        rawInput = Vector2.zero;
        var kb = Keyboard.current;
        if (InputEnabled && kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) rawInput.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) rawInput.y -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) rawInput.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) rawInput.x -= 1f;
            if (kb.spaceKey.wasPressedThisFrame) jumpBufferTimer = jumpBuffer;
        }
        if (InputEnabled)
        {
            rawInput += debugMove;
            if (debugJump) { jumpBufferTimer = jumpBuffer; debugJump = false; }
        }
        rawInput = Vector2.ClampMagnitude(rawInput, 1f); // diagonals are never faster
        MoveInput = InputModifier.Apply(rawInput);
        if (jumpBufferTimer > 0f) jumpBufferTimer -= Time.deltaTime;

        var v = rb.linearVelocity;
        var hv = new Vector3(v.x, 0f, v.z);
        HorizontalSpeed = hv.magnitude;
        if (HorizontalSpeed > 0.5f) FacingDirection = hv / HorizontalSpeed;
        else if (MoveInput.sqrMagnitude > 0.01f && cameraRig != null)
            FacingDirection = (cameraRig.FlatRight * MoveInput.x + cameraRig.FlatForward * MoveInput.y).normalized;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        var down = grav.Down;
        var up = -down;
        var velocity = rb.linearVelocity;
        float verticalBefore = Vector3.Dot(velocity, up);

        bool wasGrounded = IsGrounded;
        CheckGround(down, verticalBefore);
        if (IsGrounded) coyoteTimer = coyoteTime; else coyoteTimer -= dt;

        float vertical = verticalBefore;
        var horizontal = velocity - up * vertical;

        var target = Vector3.zero;
        if (cameraRig != null)
            target = (cameraRig.FlatRight * MoveInput.x + cameraRig.FlatForward * MoveInput.y) * moveSpeed;
        horizontal = Vector3.MoveTowards(horizontal, target, (IsGrounded ? groundAcceleration : airAcceleration) * dt);

        if (InputEnabled && jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            vertical = Mathf.Sqrt(2f * grav.gravity * jumpHeight);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            IsGrounded = false;
            Jumped?.Invoke();
            AudioManager.Play(Sfx.Jump, 0.6f);
        }

        rb.linearVelocity = horizontal + up * vertical;

        if (!wasGrounded && IsGrounded)
        {
            float impact = -verticalBefore;
            Landed?.Invoke(impact);
            if (impact > 5f) AudioManager.Play(Sfx.Land, 0.5f);
        }
    }

    void CheckGround(Vector3 down, float verticalUp)
    {
        Collider ground = null;
        if (verticalUp <= 0.5f)
        {
            float r = capsule.radius * 0.9f;
            var origin = rb.position + capsule.center;
            float dist = capsule.height * 0.5f - r + groundCheckExtra;
            if (Physics.SphereCast(origin, r, down, out var hit, dist, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider.attachedRigidbody != rb
                && Vector3.Dot(hit.normal, -down) > 0.5f)
                ground = hit.collider;
        }
        IsGrounded = ground != null;
        if (ground != GroundCollider)
        {
            GroundCollider = ground;
            onFloorSurface = ground != null && ground.GetComponentInParent<FloorSurface>() != null;
        }
    }

    public void ResetPlayer()
    {
        InputEnabled = false;
        jumpBufferTimer = 0f; coyoteTimer = 0f;
        IsGrounded = false; GroundCollider = null; onFloorSurface = false;
        FacingDirection = new Vector3(1f, 0f, 1f).normalized;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = spawnPosition;
        transform.position = spawnPosition;
        Physics.SyncTransforms();
        var visual = GetComponentInChildren<PlayerVisual>();
        if (visual != null) visual.ResetVisual();
    }

    public void OnDeath()
    {
        InputEnabled = false;
        var visual = GetComponentInChildren<PlayerVisual>();
        if (visual != null) visual.PlayDeath();
    }
}
