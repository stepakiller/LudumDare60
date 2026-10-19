using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public enum LaunchResult
{
    Launched,
    AlreadyFlying,
    NoRocketModel,
    NoTarget
}

public class RocketFlight : MonoBehaviour
{
    [SerializeField] Transform planet;
    [SerializeField] Transform rocket;
    [SerializeField] float duration = 3f;
    [SerializeField] Ease ease = Ease.InOutSine;
    [SerializeField] Vector3 modelRotationOffset;
    [SerializeField] float spinSpeed = 360f;
    [SerializeField] bool hideOnArrival = true;

    public event Action Arrived;
    public bool IsFlying { get; private set; }
    public float Progress => progress;
    public Vector3 ImpactPosition => planet.TransformPoint(path[Mathf.Max(0, count - 1)]);

    readonly Vector3[] path = new Vector3[FlightPathLine.MaxPoints];
    readonly float[] distances = new float[FlightPathLine.MaxPoints];
    readonly Vector3[] checkPath = new Vector3[FlightPathLine.MaxPoints];
    readonly Dictionary<GameObject, Transform> rocketInstances = new Dictionary<GameObject, Transform>();
    Transform current;
    int count;
    float progress;
    float spinAngle;
    Tween flightTween;

    void Awake()
    {
        if (rocket != null) rocket.gameObject.SetActive(false);
    }

    public LaunchResult CanLaunch(FlightPathLine flightPath, ChargeData charge)
    {
        if (IsFlying) return LaunchResult.AlreadyFlying;
        if (GetRocket(charge) == null) return LaunchResult.NoRocketModel;
        if (flightPath.BakePath(checkPath) < 2) return LaunchResult.NoTarget;
        return LaunchResult.Launched;
    }

    public LaunchResult Launch(FlightPathLine flightPath, ChargeData charge)
    {
        if (IsFlying) return LaunchResult.AlreadyFlying;

        Transform model = GetRocket(charge);
        if (model == null) return LaunchResult.NoRocketModel;

        count = flightPath.BakePath(path);
        if (count < 2) return LaunchResult.NoTarget;

        distances[0] = 0f;
        for (int i = 1; i < count; i++)
            distances[i] = distances[i - 1] + Vector3.Distance(path[i - 1], path[i]);

        current = model;
        progress = 0f;
        spinAngle = 0f;
        IsFlying = true;
        current.gameObject.SetActive(true);
        ApplyPose();

        flightTween?.Kill();
        flightTween = DOTween.To(() => progress, p => progress = p, 1f, duration)
                             .SetEase(ease)
                             .SetLink(gameObject)
                             .OnComplete(OnArrived);
        return LaunchResult.Launched;
    }

    Transform GetRocket(ChargeData charge)
    {
        if (charge == null || charge.rocketPrefab == null) return rocket;

        if (rocketInstances.TryGetValue(charge.rocketPrefab, out Transform instance) && instance != null)
            return instance;

        GameObject created = Instantiate(charge.rocketPrefab, transform);
        SetLayerRecursively(created.transform, planet.gameObject.layer);
        created.SetActive(false);

        instance = created.transform;
        rocketInstances[charge.rocketPrefab] = instance;
        return instance;
    }

    static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++) SetLayerRecursively(root.GetChild(i), layer);
    }

    void LateUpdate()
    {
        if (!IsFlying) return;
        spinAngle = Mathf.Repeat(spinAngle + spinSpeed * Time.deltaTime, 360f);
        ApplyPose();
    }

    void OnArrived()
    {
        progress = 1f;
        ApplyPose();
        IsFlying = false;
        if (hideOnArrival) current.gameObject.SetActive(false);
        Arrived?.Invoke();
    }

    void ApplyPose()
    {
        if (!Sample(progress, out Vector3 local, out Vector3 localForward)) return;

        Vector3 forward = planet.TransformDirection(localForward);
        Vector3 up = planet.TransformDirection(local);

        current.position = planet.TransformPoint(local);
        if (forward.sqrMagnitude > 0f)
            current.rotation = Quaternion.LookRotation(forward, up)
                               * Quaternion.AngleAxis(spinAngle, Vector3.forward)
                               * Quaternion.Euler(modelRotationOffset);
    }

    public bool TryGetPose(float t, out Vector3 position, out Vector3 forward)
    {
        position = default;
        forward = default;
        if (!Sample(t, out Vector3 local, out Vector3 localForward)) return false;

        position = planet.TransformPoint(local);
        forward = planet.TransformDirection(localForward);
        return true;
    }

    bool Sample(float t, out Vector3 local, out Vector3 localForward)
    {
        local = default;
        localForward = default;
        if (count < 2) return false;

        float target = Mathf.Clamp01(t) * distances[count - 1];

        int i = 0;
        while (i < count - 2 && distances[i + 1] < target) i++;

        float segment = distances[i + 1] - distances[i];
        float k = segment > 0f ? (target - distances[i]) / segment : 0f;

        local = Vector3.Lerp(path[i], path[i + 1], k);
        localForward = path[i + 1] - path[i];
        return true;
    }
}
