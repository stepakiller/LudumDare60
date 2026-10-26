using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-50)]
public class HeadLook : MonoBehaviour
{
    [SerializeField] Transform head;
    [SerializeField] float maxYaw = 3f;
    [SerializeField] float maxPitch = 2f;
    [SerializeField, Range(0f, 0.9f)] float deadZone = 0.15f;
    [SerializeField] AnimationCurve response = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] float sharpness = 4f;
    [SerializeField] bool freezeWhileDragging = true;

    public static bool FollowCursor = true;

    Quaternion baseRotation;
    Vector2 current;
    Vector2 target;

    void Awake()
    {
        if (head == null) head = transform;
        baseRotation = head.localRotation;
    }

    void OnDisable()
    {
        if (head != null) head.localRotation = baseRotation;
        current = Vector2.zero;
    }

    void LateUpdate()
    {
        Mouse mouse = Mouse.current;
        bool frozen = freezeWhileDragging && Cursor.lockState == CursorLockMode.Locked;

        if (!FollowCursor) target = Vector2.zero;
        else if (mouse != null && !frozen && Application.isFocused)
            target = CursorOffset(mouse.position.ReadValue());

        current = Vector2.Lerp(current, target, 1f - Mathf.Exp(-sharpness * Time.deltaTime));
        head.localRotation = baseRotation * Quaternion.Euler(-current.y * maxPitch, current.x * maxYaw, 0f);
    }

    Vector2 CursorOffset(Vector2 screenPosition)
    {
        Vector2 normalized = new Vector2(
            screenPosition.x / Mathf.Max(1, Screen.width) * 2f - 1f,
            screenPosition.y / Mathf.Max(1, Screen.height) * 2f - 1f);

        return new Vector2(Shape(normalized.x), Shape(normalized.y));
    }

    float Shape(float value)
    {
        float abs = Mathf.Clamp01(Mathf.Abs(value));
        if (abs <= deadZone) return 0f;
        float t = (abs - deadZone) / (1f - deadZone);
        return Mathf.Sign(value) * response.Evaluate(t);
    }
}
