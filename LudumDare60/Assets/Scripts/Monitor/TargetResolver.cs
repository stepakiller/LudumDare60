using System;
using UnityEngine;

public class TargetResolver : MonoBehaviour
{
    [SerializeField] Camera monitorCamera;
    [SerializeField] CrosshairController crosshair;
    [SerializeField] Transform planet;
    [SerializeField] Collider planetCollider;
    [SerializeField] float maxDistance = 1000f;

    public event Action<bool, Vector2> TargetChanged;
    public bool HasTarget { get; private set; }
    public Vector2 Coordinates { get; private set; }

    void LateUpdate()
    {
        Ray ray = monitorCamera.ViewportPointToRay(crosshair.ViewportPosition);
        bool hasTarget = planetCollider.Raycast(ray, out RaycastHit hit, maxDistance);
        Vector2 coordinates = hasTarget ? ToCoordinates(hit.point) : Vector2.zero;

        if (hasTarget == HasTarget && (coordinates - Coordinates).sqrMagnitude < 0.0001f) return;

        HasTarget = hasTarget;
        Coordinates = coordinates;
        TargetChanged?.Invoke(HasTarget, Coordinates);
    }

    Vector2 ToCoordinates(Vector3 worldPoint)
    {
        return GeoMath.FromDirection(planet.InverseTransformPoint(worldPoint));
    }
}
