using System;
using UnityEngine;
using UnityEngine.UI;

public class CrosshairController : MonoBehaviour
{
    [Header("UI (дети Canvas монитора)")]
    [SerializeField] RectTransform area;
    [SerializeField] RectTransform verticalLine;
    [SerializeField] RectTransform horizontalLine;
    [SerializeField] RectTransform ring;

    [Header("Движение")]
    [SerializeField] float speed = 0.8f;
    [SerializeField] bool invertX;
    [SerializeField] bool invertY;

    [Header("Цвет при наведении на цель")]
    [SerializeField] Graphic[] coloredParts;
    [SerializeField] Color onTargetColor = new Color(1f, 0.05f, 0.05f, 1f);

    public event Action<Vector2> PositionChanged;
    public event Action<bool> OnTargetChanged;
    public Vector2 Position { get; private set; }
    public Vector2 ViewportPosition => Position * 0.5f + new Vector2(0.5f, 0.5f);

    Vector2 input;
    Color[] normalColors;
    bool onTarget;

    void Awake()
    {
        if (coloredParts == null || coloredParts.Length == 0)
            coloredParts = new Graphic[]
            {
                ring != null ? ring.GetComponent<Graphic>() : null,
                verticalLine != null ? verticalLine.GetComponent<Graphic>() : null,
                horizontalLine != null ? horizontalLine.GetComponent<Graphic>() : null,
            };

        normalColors = new Color[coloredParts.Length];
        for (int i = 0; i < coloredParts.Length; i++)
            if (coloredParts[i] != null) normalColors[i] = coloredParts[i].color;
    }

    void Start() => ApplyPosition();

    public void SetInput(Vector2 value) => input = value;

    public void SetOnTarget(bool value)
    {
        if (value == onTarget) return;
        onTarget = value;

        for (int i = 0; i < coloredParts.Length; i++)
            if (coloredParts[i] != null) coloredParts[i].color = value ? onTargetColor : normalColors[i];

        OnTargetChanged?.Invoke(value);
    }

    void Update()
    {
        if (input.sqrMagnitude < 0.0001f) return;

        Vector2 dir = new Vector2(invertX ? -input.x : input.x, invertY ? -input.y : input.y);
        Vector2 next = Position + dir * (speed * Time.deltaTime);
        next.x = Mathf.Clamp(next.x, -1f, 1f);
        next.y = Mathf.Clamp(next.y, -1f, 1f);
        if (next == Position) return;

        Position = next;
        ApplyPosition();
        PositionChanged?.Invoke(Position);
    }

    void ApplyPosition()
    {
        Vector2 local = Vector2.Scale(Position, area.rect.size * 0.5f);

        if (verticalLine != null) verticalLine.anchoredPosition = new Vector2(local.x, verticalLine.anchoredPosition.y);
        if (horizontalLine != null) horizontalLine.anchoredPosition = new Vector2(horizontalLine.anchoredPosition.x, local.y);
        if (ring != null) ring.anchoredPosition = local;
    }
}
