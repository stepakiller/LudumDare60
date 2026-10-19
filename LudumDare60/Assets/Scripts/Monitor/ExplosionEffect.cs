using System;
using DG.Tweening;
using UnityEngine;

public class ExplosionEffect : MonoBehaviour
{
    [SerializeField] Renderer sphereRenderer;
    [SerializeField] float startScale = 0.02f;
    [SerializeField] float endScale = 0.3f;
    [SerializeField] float duration = 1f;
    [SerializeField] Ease scaleEase = Ease.OutQuad;
    [SerializeField] Ease fadeEase = Ease.InQuad;
    [SerializeField] string colorProperty = "_BaseColor";

    public event Action Finished;

    Material material;
    float startAlpha;
    bool playing;
    Sequence sequence;

    void Awake()
    {
        Init();
        if (!playing) gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (material != null) Destroy(material);
    }

    void Init()
    {
        if (material != null) return;
        if (sphereRenderer == null) sphereRenderer = GetComponent<Renderer>();
        material = sphereRenderer.material;
        startAlpha = material.GetColor(colorProperty).a;
    }

    public void Play(Vector3 worldPosition)
    {
        Init();
        sequence?.Kill();

        transform.position = worldPosition;
        transform.localScale = Vector3.one * startScale;

        Color color = material.GetColor(colorProperty);
        color.a = startAlpha;
        material.SetColor(colorProperty, color);

        playing = true;
        gameObject.SetActive(true);

        sequence = DOTween.Sequence()
            .Join(transform.DOScale(endScale, duration).SetEase(scaleEase))
            .Join(material.DOFade(0f, colorProperty, duration).SetEase(fadeEase))
            .OnComplete(Finish)
            .SetLink(gameObject);
    }

    void Finish()
    {
        playing = false;
        gameObject.SetActive(false);
        Finished?.Invoke();
    }
}
