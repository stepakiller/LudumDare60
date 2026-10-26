using UnityEngine;

public class PanelGauges : MonoBehaviour
{
    [Header("Левый: готовность к пуску")]
    [SerializeField] GaugeNeedle readinessGauge;
    [SerializeField] IndicatorLamp[] lamps;

    [Header("Средний: таймер")]
    [SerializeField] GaugeNeedle timerGauge;
    [SerializeField] MissionTimer missionTimer;

    [Header("Правый: топливо")]
    [SerializeField] GaugeNeedle fuelGauge;
    [SerializeField] RotaryKnob fuelKnob;

    int shownReady = -1;

    void OnEnable()
    {
        if (fuelKnob != null) fuelKnob.ValueChanged += OnFuelChanged;
        if (lamps != null)
            foreach (IndicatorLamp lamp in lamps)
                if (lamp != null) lamp.Switched += OnLampSwitched;
    }

    void OnDisable()
    {
        if (fuelKnob != null) fuelKnob.ValueChanged -= OnFuelChanged;
        if (lamps != null)
            foreach (IndicatorLamp lamp in lamps)
                if (lamp != null) lamp.Switched -= OnLampSwitched;
    }

    void Start()
    {
        if (fuelGauge != null && fuelKnob != null) fuelGauge.SetValue(fuelKnob.Value, true);
        if (timerGauge != null && missionTimer != null) timerGauge.SetValue(missionTimer.Remaining, true);
        UpdateReadiness(true);
    }

    void Update()
    {
        if (timerGauge != null && missionTimer != null) timerGauge.SetValue(missionTimer.Remaining);
    }

    void OnLampSwitched(bool _) => UpdateReadiness(false);

    void OnFuelChanged(int value)
    {
        if (fuelGauge != null) fuelGauge.SetValue(value);
    }

    void UpdateReadiness(bool instant)
    {
        if (readinessGauge == null || lamps == null) return;

        int ready = 0;
        foreach (IndicatorLamp lamp in lamps)
            if (lamp != null && lamp.IsOn) ready++;

        if (ready == shownReady && !instant) return;
        shownReady = ready;
        readinessGauge.SetValue(ready, instant);
    }
}
