using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HoverHintUI : MonoBehaviour
{
    public enum Placement
    {
        AboveObject,
        FollowCursor
    }

    [SerializeField] PlayerInteractor interactor;
    [SerializeField] Camera worldCamera;
    [SerializeField] float maxDistance = 10f;
    [SerializeField] LayerMask hintMask = ~0;

    [Header("Окошко")]
    [SerializeField] RectTransform panel;
    [SerializeField] CanvasGroup group;
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text descriptionText;

    [Header("Положение")]
    [SerializeField] Placement placement = Placement.AboveObject;
    [SerializeField] float gapAboveObject = 12f;
    [SerializeField] Vector2 cursorOffset = new Vector2(24f, -24f);
    [SerializeField] float edgePadding = 8f;

    [Header("Отключение после обучения")]
    [SerializeField] LaunchValidator launchValidator;
    [SerializeField] int disableAfterMissions = 1;

    [Header("Поведение")]
    [SerializeField] float showDelay = 0.25f;
    [SerializeField] float fadeDuration = 0.12f;
    [SerializeField] float popScale = 0.92f;
    [SerializeField] bool hideWhileCursorLocked = true;

    Canvas canvas;
    HoverHint current;
    Collider lastCollider;
    HoverHint lastHint;
    float hoverTime;
    bool visible;
    Tween tween;

    void Awake()
    {
        canvas = panel.GetComponentInParent<Canvas>();
        if (worldCamera == null) worldCamera = Camera.main;
        panel.pivot = placement == Placement.AboveObject ? new Vector2(0.5f, 0f) : new Vector2(0f, 1f);
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    void OnEnable()
    {
        if (launchValidator != null) launchValidator.TaskChanged += OnTaskChanged;
    }

    void OnDisable()
    {
        if (launchValidator != null) launchValidator.TaskChanged -= OnTaskChanged;
    }

    void OnTaskChanged(RocketTask _)
    {
        if (launchValidator.MissionIndex < disableAfterMissions) return;
        current = null;
        Hide();
        DOVirtual.DelayedCall(fadeDuration + 0.05f, () => enabled = false, true).SetLink(gameObject);
    }

    HoverHint FindHint()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || worldCamera == null) return null;

        Ray ray = worldCamera.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, hintMask)) return null;
        if (hit.collider != lastCollider)
        {
            lastCollider = hit.collider;
            lastHint = hit.collider.GetComponentInParent<HoverHint>();
        }
        return lastHint;
    }

    void SetCurrent(HoverHint hint)
    {
        if (hint == current) return;
        current = hint;
        hoverTime = 0f;

        if (current == null)
        {
            Hide();
            return;
        }

        Fill(current);
        if (visible) Reposition();
    }

    void Update()
    {
        bool blocked = (interactor != null && !interactor.isActiveAndEnabled)
                       || (hideWhileCursorLocked && Cursor.lockState == CursorLockMode.Locked)
                       || !Application.isFocused;

        if (!blocked) SetCurrent(FindHint());

        if (current == null || blocked)
        {
            if (blocked) hoverTime = 0f;
            Hide();
            return;
        }

        hoverTime += Time.unscaledDeltaTime;
        if (!visible && hoverTime >= showDelay) Show();
        if (visible) Reposition();
    }

    void Fill(HoverHint hint)
    {
        titleText.text = hint.Title;
        bool hasDescription = descriptionText != null && !string.IsNullOrWhiteSpace(hint.Description);
        if (descriptionText != null)
        {
            descriptionText.gameObject.SetActive(hasDescription);
            if (hasDescription) descriptionText.text = hint.Description;
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
    }

    void Show()
    {
        visible = true;
        Reposition();
        tween?.Kill();
        panel.localScale = Vector3.one * popScale;
        tween = DOTween.Sequence()
            .Join(group.DOFade(1f, fadeDuration))
            .Join(panel.DOScale(1f, fadeDuration * 1.5f).SetEase(Ease.OutBack))
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    void Hide()
    {
        if (!visible) return;
        visible = false;
        tween?.Kill();
        tween = group.DOFade(0f, fadeDuration).SetUpdate(true).SetLink(gameObject);
    }

    void Reposition()
    {
        if (placement == Placement.AboveObject && worldCamera != null) PlaceAboveObject();
        else PlaceAtCursor();
    }

    void PlaceAboveObject()
    {
        Vector3 screen = worldCamera.WorldToScreenPoint(current.GetWorldPoint());
        if (screen.z < 0f) return;

        float scale = canvas != null ? canvas.scaleFactor : 1f;
        Vector2 size = panel.rect.size * scale;
        float padding = edgePadding * scale;

        Vector2 position = new Vector2(screen.x, screen.y + gapAboveObject * scale);
        position.x = Mathf.Clamp(position.x, padding + size.x * 0.5f, Mathf.Max(padding + size.x * 0.5f, Screen.width - padding - size.x * 0.5f));
        position.y = Mathf.Clamp(position.y, padding, Mathf.Max(padding, Screen.height - padding - size.y));

        panel.position = position;
    }

    void PlaceAtCursor()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        float scale = canvas != null ? canvas.scaleFactor : 1f;
        Vector2 cursor = mouse.position.ReadValue();
        Vector2 size = panel.rect.size * scale;
        Vector2 shift = cursorOffset * scale;
        float padding = edgePadding * scale;

        Vector2 position = cursor + shift;
        if (position.x + size.x > Screen.width - padding) position.x = cursor.x - Mathf.Abs(shift.x) - size.x;
        if (position.y - size.y < padding) position.y = cursor.y + Mathf.Abs(shift.y) + size.y;
        position.x = Mathf.Clamp(position.x, padding, Mathf.Max(padding, Screen.width - padding - size.x));
        position.y = Mathf.Clamp(position.y, Mathf.Min(Screen.height - padding, size.y + padding), Screen.height - padding);

        panel.position = position;
    }
}
