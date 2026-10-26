using UnityEngine;

public class HoverHint : MonoBehaviour
{
    [SerializeField] string title;
    [SerializeField, TextArea(2, 5)] string description;

    [Header("Где показывать")]
    [SerializeField] Transform anchor;
    [SerializeField] Vector3 worldOffset = new Vector3(0f, 0.01f, 0f);

    public string Title => title;
    public string Description => description;

    Renderer[] renderers;

    public Vector3 GetWorldPoint()
    {
        if (anchor != null) return anchor.position + worldOffset;

        if (renderers == null) renderers = GetComponentsInChildren<Renderer>();

        bool found = false;
        Bounds bounds = default;
        foreach (Renderer item in renderers)
        {
            if (item == null || !item.enabled) continue;
            if (!found)
            {
                bounds = item.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(item.bounds);
            }
        }

        if (!found) return transform.position + worldOffset;
        return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z) + worldOffset;
    }
}
