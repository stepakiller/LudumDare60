using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

public class MonitorZoom : MonoBehaviour
{
    [SerializeField] Camera monitorCamera;
    [SerializeField] CinemachineCamera virtualCamera;
    [SerializeField] float zoomedOutSize = 1.5f;
    [SerializeField] float zoomedInSize = 0.3f;
    [SerializeField] float duration = 0.2f;
    [SerializeField] Ease ease = Ease.OutQuad;

    Tween zoomTween;

    public void SetZoom(float zoom, bool instant = false)
    {
        float size = zoomedOutSize * Mathf.Pow(zoomedInSize / zoomedOutSize, Mathf.Clamp01(zoom));
        zoomTween?.Kill();
        if (instant)
        {
            ApplySize(size);
            return;
        }
        zoomTween = DOTween.To(CurrentSize, ApplySize, size, duration).SetEase(ease).SetLink(gameObject);
    }

    float CurrentSize() => virtualCamera != null ? virtualCamera.Lens.OrthographicSize : monitorCamera.orthographicSize;

    void ApplySize(float size)
    {
        if (virtualCamera != null)
        {
            LensSettings lens = virtualCamera.Lens;
            lens.OrthographicSize = size;
            virtualCamera.Lens = lens;
        }
        else
        {
            monitorCamera.orthographicSize = size;
        }
    }
}
