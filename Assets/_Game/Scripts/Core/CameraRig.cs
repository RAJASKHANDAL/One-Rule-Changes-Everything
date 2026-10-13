using UnityEngine;

/// <summary>Fixed-orientation three-quarter camera that follows the player; slow cinematic orbit in the main menu.</summary>
[RequireComponent(typeof(Camera))]
public class CameraRig : MonoBehaviour
{
    public Transform target;

    [Header("Gameplay view")]
    public float distance = 23f;
    public float pitch = 52f;
    public float yaw = 45f;
    public float fieldOfView = 38f;
    public float followSharpness = 7f;
    public float focusClamp = 6.5f;

    [Header("Menu view")]
    public Vector3 menuFocus = new Vector3(4f, 0f, 1f);
    public float menuDistance = 30f;
    public float menuPitch = 38f;
    public float menuYawSwing = 9f;
    public float menuYawSpeed = 0.18f;

    Camera cam;
    Vector3 focus;
    float menuBlend = 1f, menuTarget = 1f;
    float shakeTime, shakeDuration, shakeMagnitude;

    public Vector3 FlatForward => Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
    public Vector3 FlatRight => Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.fieldOfView = fieldOfView;
    }

    public void SetMenuMode(bool menu) => menuTarget = menu ? 1f : 0f;

    public void SnapToTarget()
    {
        focus = DesiredFocus();
        menuBlend = menuTarget;
    }

    public void Shake(float magnitude, float duration)
    {
        if (!GameSettings.Shake) return;
        shakeMagnitude = magnitude; shakeDuration = duration; shakeTime = duration;
    }

    Vector3 DesiredFocus()
    {
        if (target == null) return Vector3.zero;
        var p = target.position;
        return new Vector3(Mathf.Clamp(p.x, -focusClamp, focusClamp), Mathf.Lerp(0f, p.y, 0.5f), Mathf.Clamp(p.z, -focusClamp, focusClamp));
    }

    void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;
        focus = Vector3.Lerp(focus, DesiredFocus(), 1f - Mathf.Exp(-followSharpness * dt));
        menuBlend = Mathf.MoveTowards(menuBlend, menuTarget, dt * 1.5f);

        var gameRot = Quaternion.Euler(pitch, yaw, 0f);
        var gamePos = focus - gameRot * Vector3.forward * distance;

        float menuYaw = yaw + Mathf.Sin(Time.unscaledTime * menuYawSpeed) * menuYawSwing;
        var menuRot = Quaternion.Euler(menuPitch, menuYaw, 0f);
        var menuPos = menuFocus - menuRot * Vector3.forward * menuDistance;

        float t = Mathf.SmoothStep(0f, 1f, menuBlend);
        var rot = Quaternion.Slerp(gameRot, menuRot, t);
        var pos = Vector3.Lerp(gamePos, menuPos, t);

        if (shakeTime > 0f)
        {
            shakeTime -= dt;
            pos += Random.insideUnitSphere * shakeMagnitude * Mathf.Clamp01(shakeTime / shakeDuration);
        }
        transform.SetPositionAndRotation(pos, rot);
    }
}
