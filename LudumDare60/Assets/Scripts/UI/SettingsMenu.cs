using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("Громкость")]
    [SerializeField] AudioMixer mixer;
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Slider uiSlider;
    [SerializeField] Slider ambienceSlider;

    [Header("Громкость по умолчанию (для нового игрока)")]
    [SerializeField, Range(0f, 1f)] float defaultMaster = 0.5f;
    [SerializeField, Range(0f, 1f)] float defaultMusic = 0.5f;
    [SerializeField, Range(0f, 1f)] float defaultSfx = 0.5f;
    [SerializeField, Range(0f, 1f)] float defaultUi = 0.5f;
    [SerializeField, Range(0f, 1f)] float defaultAmbience = 0.5f;

    [Header("Имена параметров в микшере")]
    [SerializeField] string masterParameter = "MasterVolume";
    [SerializeField] string musicParameter = "MusicVolume";
    [SerializeField] string sfxParameter = "SfxVolume";
    [SerializeField] string uiParameter = "UiVolume";
    [SerializeField] string ambienceParameter = "AmbienceVolume";

    [Header("Частота кадров")]
    [SerializeField] TMP_Dropdown fpsDropdown;
    [SerializeField] int[] fpsOptions = { 60, 120, 144, 160, 180, 240 };
    [SerializeField] string vSyncLabel = "VSync";

    [Header("Камера")]
    [SerializeField] Toggle headLookToggle;

    const string FpsKey = "Settings.Fps";
    const string HeadLookKey = "Settings.HeadLook";

    void Start()
    {
        BindVolume(masterSlider, masterParameter, true, defaultMaster);
        BindVolume(musicSlider, musicParameter, false, defaultMusic);
        BindVolume(sfxSlider, sfxParameter, false, defaultSfx);
        BindVolume(uiSlider, uiParameter, false, defaultUi);
        BindVolume(ambienceSlider, ambienceParameter, false, defaultAmbience);
        BindFps();
        BindHeadLook();
    }

    void BindVolume(Slider slider, string parameter, bool isMaster, float defaultValue)
    {
        string key = "Settings." + parameter;
        float value = PlayerPrefs.GetFloat(key, defaultValue);
        ApplyVolume(parameter, value, isMaster);

        if (slider == null) return;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(v =>
        {
            ApplyVolume(parameter, v, isMaster);
            PlayerPrefs.SetFloat(key, v);
        });
    }

    void ApplyVolume(string parameter, float value, bool isMaster)
    {
        if (mixer != null && !string.IsNullOrEmpty(parameter))
        {
            mixer.SetFloat(parameter, value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f);
            return;
        }

        if (isMaster) AudioListener.volume = value;
    }

    void BindFps()
    {
        int vSyncIndex = fpsOptions.Length;
        int index = Mathf.Clamp(PlayerPrefs.GetInt(FpsKey, vSyncIndex), 0, vSyncIndex);
        ApplyFps(index);

        if (fpsDropdown == null) return;

        List<string> labels = new List<string>();
        foreach (int fps in fpsOptions) labels.Add(fps.ToString());
        labels.Add(vSyncLabel);

        fpsDropdown.ClearOptions();
        fpsDropdown.AddOptions(labels);
        fpsDropdown.SetValueWithoutNotify(index);
        fpsDropdown.RefreshShownValue();
        fpsDropdown.onValueChanged.AddListener(i =>
        {
            ApplyFps(i);
            PlayerPrefs.SetInt(FpsKey, i);
        });
    }

    void ApplyFps(int index)
    {
        if (index >= fpsOptions.Length)
        {
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
            return;
        }

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = fpsOptions[index];
    }

    void BindHeadLook()
    {
        bool on = PlayerPrefs.GetInt(HeadLookKey, 1) == 1;
        HeadLook.FollowCursor = on;

        if (headLookToggle == null) return;
        headLookToggle.SetIsOnWithoutNotify(on);
        headLookToggle.onValueChanged.AddListener(value =>
        {
            HeadLook.FollowCursor = value;
            PlayerPrefs.SetInt(HeadLookKey, value ? 1 : 0);
        });
    }
}
