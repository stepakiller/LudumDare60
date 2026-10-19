using System;
using DG.Tweening;
using UnityEngine;

public class SliderLever : Interactable
{
    [Header("Значения")]
    [SerializeField] int minValue = 30;
    [SerializeField] int maxValue = 90;
    [SerializeField] int step = 15;
    [SerializeField] int startValue = 30;

    [Header("Движение рукоятки")]
    [SerializeField] Transform handle;
    [SerializeField] Vector3 localAxis = Vector3.forward;
    [SerializeField] bool useHandleAxes = true;
    [SerializeField] float travel = 0.3f;

    [Header("Мышь")]
    [SerializeField] Vector2 screenDirection = Vector2.up;
    [SerializeField] float pixelsPerStep = 40f;

    [Header("Анимация")]
    [SerializeField] float duration = 0.12f;
    [SerializeField] Ease ease = Ease.OutBack;

    public event Action<int> ValueChanged;
    public int Value => minValue + index * step;
    public float Normalized => Mathf.InverseLerp(minValue, maxValue, Value);

    Vector3 minPosition;
    Vector3 moveDirection;
    int index;
    int maxIndex;
    int dragStartIndex;
    float dragAccumulated;
    Tween moveTween;

    void Awake()
    {
        if (handle == null) handle = transform;
        step = Mathf.Max(1, step);
        maxIndex = Mathf.Max(0, (maxValue - minValue) / step);

        minPosition = handle.localPosition;
        moveDirection = useHandleAxes ? handle.localRotation * localAxis.normalized : localAxis.normalized;
        SetIndex(Mathf.RoundToInt((startValue - minValue) / (float)step), notify: false, instant: true);
    }

    public override void OnPress()
    {
        dragStartIndex = index;
        dragAccumulated = 0f;
    }

    public override void OnDrag(Vector2 mouseDelta)
    {
        if (IsLocked) return;

        dragAccumulated += Vector2.Dot(mouseDelta, screenDirection.normalized);
        dragAccumulated = Mathf.Clamp(dragAccumulated, -dragStartIndex * pixelsPerStep, (maxIndex - dragStartIndex) * pixelsPerStep);

        SetIndex(dragStartIndex + Mathf.RoundToInt(dragAccumulated / pixelsPerStep), notify: true, instant: false);
    }

    public override void OnScroll(int direction)
    {
        if (IsLocked) return;
        SetIndex(index + direction, notify: true, instant: false);
    }

    void SetIndex(int newIndex, bool notify, bool instant)
    {
        newIndex = Mathf.Clamp(newIndex, 0, maxIndex);
        if (newIndex == index && !instant) return;
        index = newIndex;

        float t = maxIndex == 0 ? 0f : index / (float)maxIndex;
        Vector3 target = minPosition + moveDirection * (travel * t);

        moveTween?.Kill();
        if (instant) handle.localPosition = target;
        else moveTween = handle.DOLocalMove(target, duration).SetEase(ease).SetLink(gameObject);

        if (notify) ValueChanged?.Invoke(Value);
    }
}
