using UnityEngine;

public class GaugeNeedle : MonoBehaviour
{
    [SerializeField] Transform needle;
    [SerializeField] Vector3 axis = Vector3.forward;

    [Header("Шкала")]
    [SerializeField] float minValue;
    [SerializeField] float maxValue = 100f;
    [SerializeField] float minAngle = -135f;
    [SerializeField] float maxAngle = 135f;

    [Header("Движение")]
    [SerializeField] float sharpness = 6f;
    [SerializeField] float jitterDegrees = 0.4f;
    [SerializeField] float jitterSpeed = 7f;

    [Header("Настройка в редакторе")]
    [SerializeField] bool preview;
    [SerializeField, Range(0f, 1f)] float previewValue;

    [SerializeField, HideInInspector] Quaternion restRotation = Quaternion.identity;
    [SerializeField, HideInInspector] bool restCaptured;

    float targetAngle;
    float currentAngle;
    float noiseSeed;
    bool initialized;

    void Awake()
    {
        if (needle == null) needle = transform;
        if (!restCaptured)
        {
            restRotation = needle.localRotation;
            restCaptured = true;
        }
        noiseSeed = Random.value * 100f;
        targetAngle = minAngle;
        currentAngle = minAngle;
    }

    public void SetValue(float value, bool instant = false)
    {
        float t = Mathf.InverseLerp(minValue, maxValue, value);
        targetAngle = Mathf.Lerp(minAngle, maxAngle, t);
        if (instant || !initialized) currentAngle = targetAngle;
        initialized = true;
    }

    void LateUpdate()
    {
        currentAngle = Mathf.Lerp(currentAngle, targetAngle, 1f - Mathf.Exp(-sharpness * Time.deltaTime));
        float jitter = jitterDegrees > 0f
            ? (Mathf.PerlinNoise(Time.time * jitterSpeed, noiseSeed) - 0.5f) * 2f * jitterDegrees
            : 0f;
        Apply(currentAngle + jitter);
    }

    void Apply(float angle) => needle.localRotation = restRotation * Quaternion.AngleAxis(angle, axis);

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying) return;
        if (needle == null) needle = transform;
        if (!restCaptured)
        {
            restRotation = needle.localRotation;
            restCaptured = true;
        }

        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null || needle == null || Application.isPlaying) return;
            needle.localRotation = preview
                ? restRotation * Quaternion.AngleAxis(Mathf.Lerp(minAngle, maxAngle, previewValue), axis)
                : restRotation;
        };
    }

    [ContextMenu("Запомнить текущий поворот стрелки как ноль")]
    void CaptureRest()
    {
        if (needle == null) needle = transform;
        preview = false;
        restRotation = needle.localRotation;
        restCaptured = true;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
