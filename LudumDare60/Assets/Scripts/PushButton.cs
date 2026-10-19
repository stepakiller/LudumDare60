using System;
using DG.Tweening;
using UnityEngine;

public class PushButton : Interactable
{
    [SerializeField] Transform cap;
    [SerializeField] Vector3 pressOffset = new Vector3(0f, -0.01f, 0f);
    [SerializeField] float pressDuration = 0.06f;
    [SerializeField] float releaseDuration = 0.12f;

    public event Action Pressed;
    public override bool LocksCursor => false;

    Vector3 basePosition;
    Sequence pressSequence;

    void Awake()
    {
        if (cap == null) cap = transform;
        basePosition = cap.localPosition;
    }

    public override void OnPress()
    {
        if (IsLocked) return;

        pressSequence?.Kill();
        cap.localPosition = basePosition;
        pressSequence = DOTween.Sequence()
            .Append(cap.DOLocalMove(basePosition + pressOffset, pressDuration).SetEase(Ease.OutQuad))
            .Append(cap.DOLocalMove(basePosition, releaseDuration).SetEase(Ease.OutBack))
            .SetLink(gameObject);

        Pressed?.Invoke();
    }
}
