using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public class MonitorCameraDirector : MonoBehaviour
{
    [Header("Камеры")]
    [SerializeField] CinemachineBrain brain;
    [SerializeField] CinemachineCamera normalCamera;
    [SerializeField] CinemachineCamera trackingCamera;
    [SerializeField] int trackingPriority = 20;

    [Header("Слежение")]
    [SerializeField] Transform planet;
    [SerializeField] RocketFlight rocketFlight;
    [SerializeField, Range(0f, 0.3f)] float lookAhead = 0.05f;
    [SerializeField] float positionSharpness = 6f;
    [SerializeField] float rotationSharpness = 4f;
    [SerializeField] bool rocketFliesUp = true;

    [Header("Зум")]
    [SerializeField] float launchSize = 0.9f;
    [SerializeField] float apexSize = 1.2f;
    [SerializeField] float impactSize = 0.45f;
    [SerializeField] float zoomSharpness = 3f;

    [Header("Взрыв")]
    [SerializeField] CinemachineImpulseSource impulseSource;
    [SerializeField] float returnDelay = 0.6f;

    [Header("Интерфейс")]
    [SerializeField] GameObject[] hideWhileTracking;

    public bool IsTracking { get; private set; }

    int idlePriority;
    float distance;
    Vector3 smoothedDirection;
    Vector3 smoothedUp;
    float smoothedSize;
    Coroutine returnRoutine;

    void Awake()
    {
        idlePriority = trackingCamera.Priority.Value;
        SetTrackingUi(false);
    }

    public void StartTracking()
    {
        if (returnRoutine != null) StopCoroutine(returnRoutine);
        returnRoutine = null;

        distance = Vector3.Distance(normalCamera.transform.position, planet.position);
        if (!rocketFlight.TryGetPose(rocketFlight.Progress, out Vector3 position, out Vector3 forward)) return;

        smoothedDirection = (position - planet.position).normalized;
        smoothedUp = PickUp(smoothedDirection, forward, normalCamera.transform.up);
        smoothedSize = launchSize;
        ApplyPose();

        IsTracking = true;
        trackingCamera.Priority = trackingPriority;
        SetTrackingUi(true);
    }

    public void Impact()
    {
        if (impulseSource != null) impulseSource.GenerateImpulse();
    }

    public void StopTracking()
    {
        if (!IsTracking) return;
        if (returnRoutine != null) StopCoroutine(returnRoutine);
        returnRoutine = StartCoroutine(ReturnRoutine());
    }

    IEnumerator ReturnRoutine()
    {
        yield return new WaitForSeconds(returnDelay);

        IsTracking = false;
        trackingCamera.Priority = idlePriority;

        yield return null;
        while (brain != null && brain.IsBlending) yield return null;

        SetTrackingUi(false);
        returnRoutine = null;
    }

    void LateUpdate()
    {
        if (!IsTracking) return;

        float progress = rocketFlight.Progress;
        if (!rocketFlight.TryGetPose(Mathf.Min(1f, progress + lookAhead), out Vector3 position, out Vector3 forward)) return;

        Vector3 direction = (position - planet.position).normalized;
        float dt = Time.deltaTime;

        smoothedDirection = Vector3.Slerp(smoothedDirection, direction, 1f - Mathf.Exp(-positionSharpness * dt)).normalized;
        Vector3 up = PickUp(smoothedDirection, forward, smoothedUp);
        smoothedUp = Vector3.Slerp(smoothedUp, up, 1f - Mathf.Exp(-rotationSharpness * dt));

        float targetSize = progress < 0.5f
            ? Mathf.Lerp(launchSize, apexSize, Mathf.SmoothStep(0f, 1f, progress * 2f))
            : Mathf.Lerp(apexSize, impactSize, Mathf.SmoothStep(0f, 1f, progress * 2f - 1f));
        smoothedSize = Mathf.Lerp(smoothedSize, targetSize, 1f - Mathf.Exp(-zoomSharpness * dt));

        ApplyPose();
    }

    Vector3 PickUp(Vector3 radial, Vector3 forward, Vector3 fallback)
    {
        Vector3 candidate = rocketFliesUp ? forward : fallback;
        Vector3 up = Vector3.ProjectOnPlane(candidate, radial);
        if (up.sqrMagnitude < 1e-6f) up = Vector3.ProjectOnPlane(fallback, radial);
        if (up.sqrMagnitude < 1e-6f) up = Vector3.ProjectOnPlane(Vector3.up, radial);
        return up.normalized;
    }

    void ApplyPose()
    {
        Transform t = trackingCamera.transform;
        t.position = planet.position + smoothedDirection * distance;
        t.rotation = Quaternion.LookRotation(-smoothedDirection, smoothedUp);

        LensSettings lens = trackingCamera.Lens;
        lens.OrthographicSize = smoothedSize;
        trackingCamera.Lens = lens;
    }

    void SetTrackingUi(bool tracking)
    {
        if (hideWhileTracking != null)
            foreach (GameObject item in hideWhileTracking)
                if (item != null) item.SetActive(!tracking);
    }
}
