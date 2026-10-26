using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] float maxDistance = 10f;
    [SerializeField] LayerMask interactableMask = ~0;
    [SerializeField] bool lockCursorWhileDragging = true;

    public event Action<Interactable> HoverChanged;
    public Interactable Hovered => hovered;

    Interactable hovered;
    Interactable dragged;
    Vector2 pressScreenPos;
    bool cursorLocked;
    Collider lastCollider;
    Interactable lastInteractable;

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (dragged != null)
        {
            dragged.OnDrag(mouse.delta.ReadValue());
            if (mouse.leftButton.wasReleasedThisFrame) EndDrag();
            return;
        }

        UpdateHover(mouse.position.ReadValue());
        if (hovered == null) return;

        float scroll = mouse.scroll.ReadValue().y;
        if (scroll != 0f) hovered.OnScroll(scroll > 0 ? 1 : -1);

        if (mouse.leftButton.wasPressedThisFrame && !hovered.IsLocked) BeginDrag(mouse);
    }

    void OnDisable()
    {
        if (dragged != null) EndDrag();
    }

    void BeginDrag(Mouse mouse)
    {
        dragged = hovered;
        pressScreenPos = mouse.position.ReadValue();
        dragged.OnPress();

        cursorLocked = lockCursorWhileDragging && dragged.LocksCursor;
        if (cursorLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void EndDrag()
    {
        dragged.OnRelease();
        dragged = null;

        if (cursorLocked)
        {
            cursorLocked = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Mouse.current?.WarpCursorPosition(pressScreenPos);
        }
    }

    void UpdateHover(Vector2 screenPos)
    {
        Interactable newHovered = null;
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out var hit, maxDistance, interactableMask))
        {
            if (hit.collider != lastCollider)
            {
                lastCollider = hit.collider;
                lastInteractable = hit.collider.GetComponentInParent<Interactable>();
            }
            newHovered = lastInteractable;
        }
        if (newHovered == hovered) return;
        if (hovered != null) hovered.OnHoverExit();
        hovered = newHovered;
        if (hovered != null) hovered.OnHoverEnter();
        HoverChanged?.Invoke(hovered);
    }
}
