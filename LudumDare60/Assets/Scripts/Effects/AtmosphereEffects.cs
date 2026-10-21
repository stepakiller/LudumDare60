using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AtmosphereEffects : MonoBehaviour
{
    [Header("Сирена")]
    [SerializeField] Siren siren;
    [SerializeField] Color sirenColor = new Color(1f, 0.35f, 0.3f);
    [SerializeField, Range(0f, 1f)] float sirenStrength = 0.8f;
    [SerializeField] float sirenPulsesPerSecond = 1.5f;
    [SerializeField] float sirenFadeOut = 0.5f;

    [Header("Вспышка взрыва")]
    [SerializeField] RocketFlight rocketFlight;
    [SerializeField] float flashExposure = 1.5f;
    [SerializeField] float flashBloom = 3f;
    [SerializeField] Color flashTint = new Color(1f, 0.85f, 0.7f);
    [SerializeField] float flashIn = 0.06f;
    [SerializeField] float flashOut = 0.9f;
    [SerializeField] Ease flashEase = Ease.OutQuad;

    [Header("Volume")]
    [SerializeField] float volumePriority = 10f;

    Volume sirenVolume;
    Volume flashVolume;
    VolumeProfile sirenProfile;
    VolumeProfile flashProfile;
    float sirenPhase;
    Tween flashTween;

    void Awake()
    {
        sirenProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        ColorAdjustments sirenAdjustments = sirenProfile.Add<ColorAdjustments>();
        sirenAdjustments.colorFilter.Override(sirenColor);
        sirenVolume = CreateVolume("Siren FX", sirenProfile);

        flashProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        ColorAdjustments flashAdjustments = flashProfile.Add<ColorAdjustments>();
        flashAdjustments.postExposure.Override(flashExposure);
        flashAdjustments.colorFilter.Override(flashTint);
        Bloom bloom = flashProfile.Add<Bloom>();
        bloom.intensity.Override(flashBloom);
        flashVolume = CreateVolume("Flash FX", flashProfile);
    }

    void OnEnable()
    {
        if (rocketFlight != null) rocketFlight.Arrived += Flash;
    }

    void OnDisable()
    {
        if (rocketFlight != null) rocketFlight.Arrived -= Flash;
    }

    void OnDestroy()
    {
        if (sirenProfile != null) Destroy(sirenProfile);
        if (flashProfile != null) Destroy(flashProfile);
    }

    void Update()
    {
        if (siren != null && siren.IsOn)
        {
            sirenPhase += Time.deltaTime * sirenPulsesPerSecond;
            sirenVolume.weight = sirenStrength * (0.5f - 0.5f * Mathf.Cos(sirenPhase * Mathf.PI * 2f));
            return;
        }

        sirenPhase = 0f;
        if (sirenVolume.weight > 0f)
            sirenVolume.weight = Mathf.MoveTowards(sirenVolume.weight, 0f, Time.deltaTime / Mathf.Max(0.01f, sirenFadeOut));
    }

    [ContextMenu("Вспышка")]
    public void Flash()
    {
        flashTween?.Kill();
        flashVolume.weight = 0f;
        flashTween = DOTween.Sequence()
            .Append(DOTween.To(() => flashVolume.weight, w => flashVolume.weight = w, 1f, flashIn))
            .Append(DOTween.To(() => flashVolume.weight, w => flashVolume.weight = w, 0f, flashOut).SetEase(flashEase))
            .SetLink(gameObject);
    }

    Volume CreateVolume(string volumeName, VolumeProfile profile)
    {
        GameObject child = new GameObject(volumeName);
        child.transform.SetParent(transform, false);
        child.layer = gameObject.layer;

        Volume volume = child.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = volumePriority;
        volume.weight = 0f;
        volume.sharedProfile = profile;
        return volume;
    }
}
