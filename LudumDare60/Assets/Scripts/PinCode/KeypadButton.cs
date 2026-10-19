using System;
using DG.Tweening;
using UnityEngine;

public class KeypadButton : Interactable
{
    [SerializeField] int digit;
    [SerializeField] Transform cap;
    [SerializeField] Vector3 pressOffset = new Vector3(0f, -0.01f, 0f);
    [SerializeField] float duration = 0.08f;

    public event Action<int> Pressed;
    public override bool LocksCursor => false;
    public int Digit => digit;
    public bool IsHeld { get; private set; }

    Vector3 basePosition;
    Tween moveTween;

    void Awake()
    {
        if (cap == null) cap = transform;
        basePosition = cap.localPosition;
    }

    public override void OnPress()
    {
        if (IsLocked) return;
        Pressed?.Invoke(digit);
    }

    public void SetHeld(bool held)
    {
        if (held && IsHeld)
        {
            moveTween?.Kill();
            cap.localPosition = basePosition + pressOffset;
            moveTween = cap.DOPunchPosition(pressOffset * 0.5f, duration * 2f, 1, 0f).SetLink(gameObject);
            return;
        }

        IsHeld = held;
        moveTween?.Kill();
        Vector3 target = held ? basePosition + pressOffset : basePosition;
        moveTween = cap.DOLocalMove(target, duration).SetEase(held ? Ease.OutQuad : Ease.OutBack).SetLink(gameObject);
    }
}
