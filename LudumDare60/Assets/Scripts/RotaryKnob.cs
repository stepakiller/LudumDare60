using System;
using DG.Tweening;
using UnityEngine;

public class RotaryKnob : Interactable
{
    [Header("Значения")]
    [SerializeField] int minValue = 0;
    [SerializeField] int maxValue = 140;
    [SerializeField] int step = 20;
    [SerializeField] int startValue = 0;

    [Header("Поворот")]
    [SerializeField] Transform pivot;
    [SerializeField] Vector3 axis = Vector3.forward;
    [SerializeField] float minAngle = -90f;
    [SerializeField] float maxAngle = 90f;

    [SerializeField] float duration = 0.15f;
    [SerializeField] Ease ease = Ease.OutBack;

    public event Action<int> ValueChanged;
    public int Value { get; private set; }
    public float Normalized => Mathf.InverseLerp(minValue, maxValue, Value);

    Quaternion baseRotation;
    float currentAngle;
    Tween rotateTween;

    void Awake()
    {
        if (pivot == null) pivot = transform;
        baseRotation = pivot.localRotation;
        SetValue(startValue, notify: false, instant: true);
    }

    public override void OnScroll(int direction)
    {
        if (IsLocked) return;
        SetValue(Value + direction * step, notify: true, instant: false);
    }

    void SetValue(int newValue, bool notify, bool instant)
    {
        int clamped = Mathf.Clamp(newValue, minValue, maxValue);
        bool changed = clamped != Value;
        Value = clamped;
        float t = Mathf.InverseLerp(minValue, maxValue, Value);
        RotateTo(Mathf.Lerp(minAngle, maxAngle, t), instant);
        if (notify && changed) ValueChanged?.Invoke(Value);
    }

    void RotateTo(float targetAngle, bool instant)
    {
        rotateTween?.Kill();
        if (instant)
        {
            currentAngle = targetAngle;
            ApplyRotation();
            return;
        }
        rotateTween = DOTween.To(() => currentAngle, a => { currentAngle = a; ApplyRotation(); }, targetAngle, duration).SetEase(ease).SetLink(gameObject);
    }

    void ApplyRotation() => pivot.localRotation = baseRotation * Quaternion.AngleAxis(currentAngle, axis);
}
