using System.Globalization;
using TMPro;
using UnityEngine;

public class MissionSheet : MonoBehaviour
{
    [SerializeField] LaunchValidator launchValidator;

    [Header("Тексты листа")]
    [SerializeField] TMP_Text titleText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] TMP_Text informationText;

    [Header("Формат")]
    [SerializeField, TextArea(4, 10)] string informationTemplate =
        "Coordinates:\n{LAT} {LAT_H} {LON} {LON_H}\n\nTrajectory: {TRAJECTORY}°\n\nRocket type: {CHARGE}";
    [SerializeField] string coordinatesFormat = "0.0000";
    [SerializeField] bool positiveLatitudeIsSouth = true;
    [SerializeField] string anyChargeText = "Any";

    void OnEnable() => launchValidator.TaskChanged += Show;

    void OnDisable() => launchValidator.TaskChanged -= Show;

    void Start() => Show(launchValidator.CurrentTask);

    void Show(RocketTask task)
    {
        if (task == null) return;

        if (titleText != null) titleText.text = task.missionName;
        if (descriptionText != null) descriptionText.text = task.description;
        if (informationText == null) return;

        float lat = task.targetLatitude;
        float lon = task.targetLongitude;
        string latHemisphere = (lat >= 0f) == positiveLatitudeIsSouth ? "S" : "N";
        string lonHemisphere = lon >= 0f ? "E" : "W";

        informationText.text = informationTemplate
            .Replace("{LAT}", Mathf.Abs(lat).ToString(coordinatesFormat, CultureInfo.InvariantCulture))
            .Replace("{LAT_H}", latHemisphere)
            .Replace("{LON}", Mathf.Abs(lon).ToString(coordinatesFormat, CultureInfo.InvariantCulture))
            .Replace("{LON_H}", lonHemisphere)
            .Replace("{TRAJECTORY}", task.trajectory.ToString(CultureInfo.InvariantCulture))
            .Replace("{CHARGE}", task.charge != null ? task.charge.chargeName : anyChargeText);
    }
}
