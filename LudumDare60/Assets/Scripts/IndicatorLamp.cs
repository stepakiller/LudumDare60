using System;
using DG.Tweening;
using UnityEngine;

public class IndicatorLamp : MonoBehaviour
{
    [SerializeField] Renderer lampRenderer;
    [SerializeField] Material offMaterial;
    [SerializeField] Material onMaterial;
    [SerializeField] float punchScale = 0.15f;
    [SerializeField] float punchDuration = 0.25f;

    public event Action<bool> Switched;
    public bool IsOn { get; private set; }

    bool initialized;
    Vector3 baseScale;
    Tween punchTween;

    void Awake()
    {
        if (lampRenderer == null) lampRenderer = GetComponent<Renderer>();
        baseScale = transform.localScale;
        ApplyMaterial();
    }

    public void SetOn(bool value)
    {
        if (initialized && value == IsOn) return;

        bool justTurnedOn = initialized && value;
        initialized = true;
        IsOn = value;
        ApplyMaterial();
        Switched?.Invoke(value);

        if (!justTurnedOn || punchScale <= 0f) return;

        punchTween?.Kill();
        transform.localScale = baseScale;
        punchTween = transform.DOPunchScale(baseScale * punchScale, punchDuration, 6, 0.5f).SetLink(gameObject);
    }

    void ApplyMaterial()
    {
        if (lampRenderer != null) lampRenderer.sharedMaterial = IsOn ? onMaterial : offMaterial;
    }
}
