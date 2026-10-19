using System;
using DG.Tweening;
using UnityEngine;

public class Joystick : Interactable
{
    [SerializeField] float sensitivity = 0.01f;
    [SerializeField] Transform pivot;
    [SerializeField] float maxTilt = 25f;
    [SerializeField] Vector3 tiltAxisX = Vector3.forward;
    [SerializeField] Vector3 tiltAxisY = Vector3.right;
    [SerializeField] float returnDuration = 0.25f;
    [SerializeField] Ease returnEase = Ease.OutBack;
    public event Action<Vector2> ValueChanged;
    public Vector2 Value { get; private set; }

    Quaternion baseRotation;
    Tween returnTween;

    void Awake()
    {
        if (pivot == null) pivot = transform;
        baseRotation = pivot.localRotation;
    }

    public override void OnPress() => returnTween?.Kill();

    public override void OnDrag(Vector2 mouseDelta)
    {
        if (IsLocked) return;
        Vector2 next = Vector2.ClampMagnitude(Value + mouseDelta * sensitivity, 1f);
        if (next == Value) return;
        SetValue(next);
    }

    public override void OnRelease()
    {
        if (IsLocked) return;
        returnTween?.Kill();
        returnTween = DOTween.To(() => Value, SetValue, Vector2.zero, returnDuration)
                             .SetEase(returnEase)
                             .SetLink(gameObject);
    }

    protected override void OnLockedChanged(bool locked)
    {
        if (!locked) return;

        returnTween?.Kill();
        Value = Vector2.zero;
        ValueChanged?.Invoke(Value);
        returnTween = pivot.DOLocalRotateQuaternion(baseRotation, returnDuration)
                           .SetEase(returnEase)
                           .SetLink(gameObject);
    }

    void SetValue(Vector2 value)
    {
        Value = value;
        pivot.localRotation = baseRotation
                              * Quaternion.AngleAxis(-value.x * maxTilt, tiltAxisX)
                              * Quaternion.AngleAxis(value.y * maxTilt, tiltAxisY);
        ValueChanged?.Invoke(Value);
    }
}
