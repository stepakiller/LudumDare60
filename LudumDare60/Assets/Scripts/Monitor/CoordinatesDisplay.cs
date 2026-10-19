using TMPro;
using UnityEngine;

public class CoordinatesDisplay : MonoBehaviour
{
    [SerializeField] TMP_Text latitudeValue;
    [SerializeField] TMP_Text latitudeHemisphere;
    [SerializeField] TMP_Text longitudeValue;
    [SerializeField] TMP_Text longitudeHemisphere;
    [SerializeField] string noTargetValue = "--.----";

    public void Show(bool hasTarget, Vector2 coordinates)
    {
        if (!hasTarget)
        {
            latitudeValue.text = noTargetValue;
            longitudeValue.text = noTargetValue;
            latitudeHemisphere.text = "-";
            longitudeHemisphere.text = "-";
            return;
        }

        float lon = coordinates.x;
        float lat = coordinates.y;

        latitudeValue.SetText("{0:4}", Mathf.Abs(lat));
        longitudeValue.SetText("{0:4}", Mathf.Abs(lon));
        latitudeHemisphere.text = lat >= 0f ? "S" : "N";
        longitudeHemisphere.text = lon >= 0f ? "E" : "W";
    }
}
