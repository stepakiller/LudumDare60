using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class FlightPathLine : MonoBehaviour
{
    [SerializeField] Transform planet;
    [SerializeField] Transform baseMarker;
    [SerializeField] float heightMultiplier = 1.01f;
    [SerializeField] float degreesPerPoint = 2f;

    [Header("Высота дуги")]
    [SerializeField] float heightScale = 1f;
    [SerializeField] float maxApex = 0.6f;
    [SerializeField] float maxAngle = 85f;
    [SerializeField] float angleChangeDuration = 0.3f;

    [Header("Пунктир")]
    [SerializeField] float width = 0.01f;
    [SerializeField] float dashesPerUnit = 20f;
    [SerializeField] float scrollSpeed = 1f;

    static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");
    public const int MaxPoints = 182;

    LineRenderer line;
    MaterialPropertyBlock block;
    readonly Vector3[] points = new Vector3[MaxPoints];
    Vector3 baseDirection;
    float radius;
    float offset;

    bool hasTarget;
    bool hidden;
    Vector3 targetDirection;
    float launchAngle = 45f;
    Tween angleTween;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.textureMode = LineTextureMode.Tile;
        line.textureScale = new Vector2(dashesPerUnit, 1f);
        line.widthMultiplier = width;
        line.positionCount = 0;
        line.enabled = false;

        Vector3 local = planet.InverseTransformPoint(baseMarker.position);
        baseDirection = local.normalized;
        radius = local.magnitude * heightMultiplier;

        block = new MaterialPropertyBlock();
    }

    public void SetTarget(bool value, Vector2 coordinates)
    {
        hasTarget = value;
        line.enabled = hasTarget && !hidden;
        if (!value) return;

        targetDirection = GeoMath.ToDirection(coordinates);
        Rebuild();
    }

    public void SetTrajectory(float angle, bool instant = false)
    {
        angleTween?.Kill();
        if (instant)
        {
            launchAngle = angle;
            Rebuild();
            return;
        }

        angleTween = DOTween.To(() => launchAngle, a => { launchAngle = a; Rebuild(); }, angle, angleChangeDuration)
                            .SetEase(Ease.OutQuad)
                            .SetLink(gameObject);
    }

    public void SetHidden(bool value)
    {
        if (hidden == value) return;
        hidden = value;
        line.enabled = hasTarget && !hidden;
        if (!hidden) Rebuild();
    }

    public int BakePath(Vector3[] buffer) => hasTarget ? ComputePath(buffer) : 0;

    void Rebuild()
    {
        if (!hasTarget || hidden) return;

        int count = ComputePath(points);
        for (int i = 0; i < count; i++)
            points[i] = transform.InverseTransformPoint(planet.TransformPoint(points[i]));

        line.positionCount = count;
        line.SetPositions(points);
    }

    int ComputePath(Vector3[] buffer)
    {
        float angleRad = Vector3.Angle(baseDirection, targetDirection) * Mathf.Deg2Rad;
        float range = angleRad * radius;

        float tan = Mathf.Tan(Mathf.Min(launchAngle, maxAngle) * Mathf.Deg2Rad);
        float apex = Mathf.Min(range * tan * 0.25f * heightScale, maxApex * radius);

        int count = Mathf.Clamp(Mathf.CeilToInt(angleRad * Mathf.Rad2Deg / degreesPerPoint) + 1, 2, Mathf.Min(MaxPoints, buffer.Length));
        for (int i = 0; i < count; i++)
        {
            float t = i / (count - 1f);
            Vector3 direction = Vector3.Slerp(baseDirection, targetDirection, t);
            float height = apex * 4f * t * (1f - t);
            buffer[i] = direction * (radius + height);
        }

        return count;
    }

    void Update()
    {
        if (scrollSpeed == 0f || !line.enabled) return;

        offset = Mathf.Repeat(offset - scrollSpeed * Time.deltaTime, 1f);
        line.GetPropertyBlock(block);
        block.SetVector(BaseMapST, new Vector4(1f, 1f, offset, 0f));
        line.SetPropertyBlock(block);
    }
}
