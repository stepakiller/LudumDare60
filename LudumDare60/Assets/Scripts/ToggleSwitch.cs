using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class ToggleSwitch : Interactable
{
    [Serializable]
    public class EmissionTarget
    {
        public Renderer renderer;
        public int materialIndex;
    }

    class LightGroup
    {
        public Light[] Lights;
        public float[] Intensities;
    }

    [Header("Рычаг")]
    [SerializeField] Transform lever;
    [SerializeField] Vector3 axis = Vector3.right;
    [SerializeField] float throwAngle = 60f;
    [SerializeField] float throwDuration = 0.1f;
    [SerializeField] Ease throwEase = Ease.InExpo;

    [Header("Состояние")]
    [SerializeField] bool startOn = true;

    [Header("Объекты")]
    [SerializeField] GameObject[] enableWhenOn;
    [SerializeField] GameObject[] enableWhenOff;

    [Header("Свечение материалов")]
    [SerializeField] EmissionTarget[] emission;
    [SerializeField] string emissionProperty = "_EmissionColor";

    [Header("Плавность")]
    [SerializeField] float fadeInDuration = 0.4f;
    [SerializeField] float fadeOutDuration = 0.6f;
    [SerializeField] Ease fadeInEase = Ease.InQuad;
    [SerializeField] Ease fadeOutEase = Ease.OutQuad;

    [Header("Дополнительно")]
    [SerializeField] UnityEvent onSwitchedOn;
    [SerializeField] UnityEvent onSwitchedOff;

    public event Action<bool> Toggled;
    public bool IsOn { get; private set; }
    public override bool LocksCursor => false;

    Quaternion restRotation;
    Color[] emissionColors;
    MaterialPropertyBlock block;
    int emissionId;
    LightGroup onLights;
    LightGroup offLights;
    float level;
    Tween leverTween;
    Tween fadeTween;

    void Awake()
    {
        if (lever == null) lever = transform;
        restRotation = lever.localRotation;
        block = new MaterialPropertyBlock();
        emissionId = Shader.PropertyToID(emissionProperty);

        emissionColors = new Color[emission != null ? emission.Length : 0];
        for (int i = 0; i < emissionColors.Length; i++)
            emissionColors[i] = ReadEmission(emission[i]);

        onLights = CollectLights(enableWhenOn);
        offLights = CollectLights(enableWhenOff);
    }

    void Start() => Apply(startOn, true, false);

    public override void OnPress()
    {
        if (IsLocked) return;
        Apply(!IsOn, false, true);
    }

    public void SetState(bool on, bool instant = false)
    {
        if (on == IsOn) return;
        Apply(on, instant, true);
    }

    [ContextMenu("Переключить")]
    void ToggleFromMenu() => Apply(!IsOn, false, true);

    void Apply(bool on, bool instant, bool notify)
    {
        IsOn = on;

        Quaternion target = restRotation * Quaternion.AngleAxis(on == startOn ? 0f : throwAngle, axis);
        leverTween?.Kill();
        if (instant) lever.localRotation = target;
        else leverTween = lever.DOLocalRotateQuaternion(target, throwDuration).SetEase(throwEase).SetLink(gameObject);

        Fade(on, instant);

        if (!notify) return;
        Toggled?.Invoke(on);
        if (on) onSwitchedOn?.Invoke();
        else onSwitchedOff?.Invoke();
    }

    void Fade(bool on, bool instant)
    {
        fadeTween?.Kill();
        float target = on ? 1f : 0f;
        float duration = on ? fadeInDuration : fadeOutDuration;

        SetObjects(on ? enableWhenOn : enableWhenOff, true);

        if (instant || duration <= 0f)
        {
            SetLevel(target);
            FinishFade();
            return;
        }

        SetLevel(level);
        fadeTween = DOTween.To(() => level, SetLevel, target, duration)
                           .SetEase(on ? fadeInEase : fadeOutEase)
                           .OnComplete(FinishFade)
                           .SetLink(gameObject);
    }

    void FinishFade()
    {
        if (level >= 1f) SetObjects(enableWhenOff, false);
        if (level <= 0f) SetObjects(enableWhenOn, false);
    }

    void SetLevel(float value)
    {
        level = value;
        ApplyLights(onLights, value);
        ApplyLights(offLights, 1f - value);

        if (emission == null) return;
        for (int i = 0; i < emission.Length; i++)
        {
            EmissionTarget item = emission[i];
            if (item == null || item.renderer == null) continue;
            item.renderer.GetPropertyBlock(block, item.materialIndex);
            block.SetColor(emissionId, emissionColors[i] * value);
            item.renderer.SetPropertyBlock(block, item.materialIndex);
        }
    }

    static void ApplyLights(LightGroup group, float factor)
    {
        for (int i = 0; i < group.Lights.Length; i++)
            if (group.Lights[i] != null) group.Lights[i].intensity = group.Intensities[i] * factor;
    }

    static LightGroup CollectLights(GameObject[] objects)
    {
        System.Collections.Generic.List<Light> found = new System.Collections.Generic.List<Light>();
        if (objects != null)
            foreach (GameObject item in objects)
                if (item != null) found.AddRange(item.GetComponentsInChildren<Light>(true));

        LightGroup group = new LightGroup { Lights = found.ToArray(), Intensities = new float[found.Count] };
        for (int i = 0; i < found.Count; i++) group.Intensities[i] = found[i].intensity;
        return group;
    }

    Color ReadEmission(EmissionTarget item)
    {
        if (item == null || item.renderer == null) return Color.black;
        Material[] materials = item.renderer.sharedMaterials;
        if (item.materialIndex < 0 || item.materialIndex >= materials.Length) return Color.black;
        Material material = materials[item.materialIndex];
        return material != null && material.HasProperty(emissionId) ? material.GetColor(emissionId) : Color.black;
    }

    static void SetObjects(GameObject[] objects, bool active)
    {
        if (objects == null) return;
        foreach (GameObject item in objects)
            if (item != null) item.SetActive(active);
    }
}
