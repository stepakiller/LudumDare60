using System;
using UnityEngine;

public class GlobeRotator : MonoBehaviour
{
    [SerializeField] Transform globe;
    [SerializeField] float degreesPerSecond = 60f;
    [SerializeField] float maxLatitude = 80f;

    [SerializeField] Vector3 longitudeAxis = Vector3.up;
    [SerializeField] Vector3 latitudeAxis = Vector3.forward;

    public event Action<Vector2> CoordinatesChanged;
    public Vector2 Coordinates => new Vector2(longitude, latitude);

    Quaternion baseRotation;
    float longitude;
    float latitude;
    Vector2 input;

    void Awake()
    {
        if (globe == null) globe = transform;
        baseRotation = globe.localRotation;
        ApplyRotation();
    }

    public void SetInput(Vector2 value) => input = value;

    void Update()
    {
        if (input.sqrMagnitude < 0.0001f) return;
        float step = degreesPerSecond * Time.deltaTime;
        longitude = Mathf.Repeat(longitude + input.x * step + 180f, 360f) - 180f;
        latitude = Mathf.Clamp(latitude - input.y * step, -maxLatitude, maxLatitude);
        ApplyRotation();
        CoordinatesChanged?.Invoke(Coordinates);
    }

    void ApplyRotation() => globe.localRotation = baseRotation * Quaternion.AngleAxis(latitude, latitudeAxis) * Quaternion.AngleAxis(-longitude, longitudeAxis);
}
