using DG.Tweening;
using UnityEngine;

public class LaunchCover : MonoBehaviour
{
    [SerializeField] Transform cover;
    [SerializeField] Vector3 openEulerAngles = new Vector3(0f, 90f, 180f);
    [SerializeField] float duration = 0.5f;
    [SerializeField] Ease openEase = Ease.OutBack;
    [SerializeField] float openOvershoot = 1.70158f;
    [SerializeField] Ease closeEase = Ease.InOutQuad;

    public bool IsOpen { get; private set; }

    Quaternion closedRotation;
    Quaternion openRotation;
    Tween rotateTween;

    void Awake()
    {
        if (cover == null) cover = transform;
        closedRotation = cover.localRotation;
        openRotation = Quaternion.Euler(openEulerAngles);
    }

    public void SetOpen(bool open)
    {
        if (open == IsOpen) return;
        IsOpen = open;

        rotateTween?.Kill();
        Quaternion from = cover.localRotation;
        Quaternion to = open ? openRotation : closedRotation;
        rotateTween = DOTween.To(t => cover.localRotation = Quaternion.SlerpUnclamped(from, to, t), 0f, 1f, duration)
                             .SetEase(open ? openEase : closeEase, open ? openOvershoot : 1.70158f)
                             .SetLink(gameObject);
    }
}
