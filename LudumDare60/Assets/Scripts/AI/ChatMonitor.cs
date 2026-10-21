using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ChatMonitor : Interactable
{
    [Header("Камера")]
    [SerializeField] Transform cameraTransform;
    [SerializeField] Transform focusPoint;
    [SerializeField] float duration = 0.6f;
    [SerializeField] Ease ease = Ease.InOutSine;

    [Header("Чат")]
    [SerializeField] TMP_InputField input;

    [Header("Отключать при приближении")]
    [SerializeField] HeadLook headLook;
    [SerializeField] PlayerInteractor playerInteractor;
    [SerializeField] Behaviour[] disableWhileFocused;

    public override bool LocksCursor => false;
    public bool IsFocused { get; private set; }

    Vector3 savedPosition;
    Quaternion savedRotation;
    Sequence moveSequence;
    bool moving;
    bool focusRequested;

    void Awake()
    {
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        if (input != null) input.restoreOriginalTextOnEscape = false;
    }

    public override void OnPress()
    {
        if (IsLocked || IsFocused || moving) return;
        focusRequested = true;
    }

    void Update()
    {
        if (focusRequested)
        {
            focusRequested = false;
            Focus();
            return;
        }

        if (!IsFocused) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Unfocus();
            return;
        }

        if (!moving && input != null && !input.isFocused) input.ActivateInputField();
    }

    void Focus()
    {
        IsFocused = true;
        SetOthersEnabled(false);

        savedPosition = cameraTransform.position;
        savedRotation = cameraTransform.rotation;

        Move(focusPoint.position, focusPoint.rotation, () =>
        {
            if (input != null) input.ActivateInputField();
        });
    }

    void Unfocus()
    {
        IsFocused = false;

        if (input != null) input.DeactivateInputField();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        Move(savedPosition, savedRotation, () => SetOthersEnabled(true));
    }

    void Move(Vector3 position, Quaternion rotation, TweenCallback onComplete)
    {
        moveSequence?.Kill();
        moving = true;
        moveSequence = DOTween.Sequence()
            .Join(cameraTransform.DOMove(position, duration).SetEase(ease))
            .Join(cameraTransform.DORotateQuaternion(rotation, duration).SetEase(ease))
            .OnComplete(() =>
            {
                moving = false;
                onComplete?.Invoke();
            })
            .SetLink(gameObject);
    }

    void SetOthersEnabled(bool enabled)
    {
        if (headLook != null) headLook.enabled = enabled;
        if (playerInteractor != null) playerInteractor.enabled = enabled;
        if (disableWhileFocused == null) return;
        foreach (Behaviour behaviour in disableWhileFocused)
            if (behaviour != null) behaviour.enabled = enabled;
    }
}
