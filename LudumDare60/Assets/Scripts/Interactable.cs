using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    bool isLocked;

    public bool IsLocked
    {
        get => isLocked;
        set
        {
            if (isLocked == value) return;
            isLocked = value;
            OnLockedChanged(value);
        }
    }

    protected virtual void OnLockedChanged(bool locked) { }

    public virtual bool LocksCursor => true;

    public virtual void OnHoverEnter() { }
    public virtual void OnHoverExit() { }
    public virtual void OnScroll(int direction) { }

    public virtual void OnPress() { }
    public virtual void OnDrag(Vector2 mouseDelta) { }
    public virtual void OnRelease() { }
}
