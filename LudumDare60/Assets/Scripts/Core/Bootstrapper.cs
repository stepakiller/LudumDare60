using TMPro;
using UnityEngine;

public class Bootstrapper : MonoBehaviour
{
    [SerializeField] CurrentConfig config;
    [SerializeField] RotaryKnob fuelKnob;
    [SerializeField] SliderLever trajectoryLever;
    [SerializeField] Joystick coordinatesJoystick;
    [SerializeField] GlobeRotator globe;

    [Header("Прицел")]
    [SerializeField] Joystick crosshairJoystick;
    [SerializeField] CrosshairController crosshair;
    [SerializeField] TargetResolver targetResolver;
    [SerializeField] CoordinatesDisplay coordinatesDisplay;
    [SerializeField] LaunchValidator launchValidator;
    [SerializeField] FuelComputer fuelComputer;
    [SerializeField] FlightPathLine flightPath;
    [SerializeField] TMP_Text rocketNameText;

    [Header("Заряды")]
    [SerializeField] PushButton chargeUpButton;
    [SerializeField] PushButton chargeConfirmButton;
    [SerializeField] PushButton chargeDownButton;
    [SerializeField] ChargeSelector chargeSelector;
    [SerializeField] ChargesScreen chargesScreen;
    [SerializeField] TMP_Text chargeNameText;
    [SerializeField] string noChargeText = "None";
    [SerializeField] Interactable[] lockWhileChargesOpen;

    [Header("Индикаторы")]
    [SerializeField] IndicatorLamp coordinatesLamp;
    [SerializeField] IndicatorLamp trajectoryLamp;
    [SerializeField] IndicatorLamp fuelLamp;
    [SerializeField] IndicatorLamp chargeLamp;
    [SerializeField] IndicatorLamp pinCodeLamp;

    [Header("Пин-код")]
    [SerializeField] PinCodeKeypad pinKeypad;
    
    [Header("Готовность к пуску")]
    [SerializeField] Interactable[] lockWhenReady;

    [Header("Пуск")]
    [SerializeField] PushButton launchButton;
    [SerializeField] RocketFlight rocketFlight;
    [SerializeField] ExplosionEffect explosion;
    [SerializeField] LaunchCover launchCover;
    [SerializeField] MonitorCameraDirector cameraDirector;
    [SerializeField] MissionTimer missionTimer;

    [Header("Зум монитора")]
    [SerializeField] RotaryKnob zoomKnob;
    [SerializeField] MonitorZoom monitorZoom;

    void OnEnable()
    {
        fuelKnob.ValueChanged += OnFuelChanged;
        trajectoryLever.ValueChanged += OnTrajectoryChanged;
        coordinatesJoystick.ValueChanged += globe.SetInput;
        crosshairJoystick.ValueChanged += crosshair.SetInput;
        zoomKnob.ValueChanged += OnZoomChanged;
        targetResolver.TargetChanged += OnTargetChanged;
        launchValidator.TaskChanged += OnTaskChanged;
        chargeUpButton.Pressed += chargeSelector.PressUp;
        chargeConfirmButton.Pressed += chargeSelector.PressConfirm;
        chargeDownButton.Pressed += chargeSelector.PressDown;
        chargeSelector.Opened += OnChargesOpened;
        chargeSelector.Closed += OnChargesClosed;
        chargeSelector.HighlightChanged += chargesScreen.SetHighlight;
        chargeSelector.ChargeChosen += OnChargeChosen;
        pinKeypad.CodeEntered += OnPinCodeEntered;
        launchButton.Pressed += OnLaunchPressed;
        rocketFlight.Arrived += OnRocketArrived;
        explosion.Finished += OnExplosionFinished;
    }

    void OnDisable()
    {
        fuelKnob.ValueChanged -= OnFuelChanged;
        trajectoryLever.ValueChanged -= OnTrajectoryChanged;
        coordinatesJoystick.ValueChanged -= globe.SetInput;
        crosshairJoystick.ValueChanged -= crosshair.SetInput;
        zoomKnob.ValueChanged -= OnZoomChanged;
        targetResolver.TargetChanged -= OnTargetChanged;
        launchValidator.TaskChanged -= OnTaskChanged;
        chargeUpButton.Pressed -= chargeSelector.PressUp;
        chargeConfirmButton.Pressed -= chargeSelector.PressConfirm;
        chargeDownButton.Pressed -= chargeSelector.PressDown;
        chargeSelector.Opened -= OnChargesOpened;
        chargeSelector.Closed -= OnChargesClosed;
        chargeSelector.HighlightChanged -= chargesScreen.SetHighlight;
        chargeSelector.ChargeChosen -= OnChargeChosen;
        pinKeypad.CodeEntered -= OnPinCodeEntered;
        launchButton.Pressed -= OnLaunchPressed;
        rocketFlight.Arrived -= OnRocketArrived;
        explosion.Finished -= OnExplosionFinished;
    }

    void Start()
    {
        OnFuelChanged(fuelKnob.Value);
        config.currentTrajectory = trajectoryLever.Value;
        flightPath.SetTrajectory(trajectoryLever.Value, instant: true);
        monitorZoom.SetZoom(zoomKnob.Normalized, instant: true);
        OnTaskChanged(launchValidator.CurrentTask);
        chargesScreen.Build(chargeSelector.Charges, chargeSelector.FirstAtBottom);
        config.currentCharge = chargeSelector.Selected;
        ShowChargeName(config.currentCharge);
        RefreshIndicators();
    }

    void OnFuelChanged(int value)
    {
        config.currentFuel = value;
        fuelComputer.SetCurrentFuel(value);
        RefreshIndicators();
    }

    void OnTrajectoryChanged(int value)
    {
        config.currentTrajectory = value;
        flightPath.SetTrajectory(value);
        RefreshIndicators();
    }

    void OnZoomChanged(int _) => monitorZoom.SetZoom(zoomKnob.Normalized);

    void OnChargesOpened(int highlightIndex)
    {
        SetControlsLocked(true);
        chargesScreen.Show(highlightIndex);
    }

    void OnChargesClosed()
    {
        SetControlsLocked(false);
        chargesScreen.Hide();
    }

    void OnChargeChosen(ChargeData charge)
    {
        config.currentCharge = charge;
        ShowChargeName(charge);
        RefreshIndicators();
    }

    void ShowChargeName(ChargeData charge)
    {
        if (chargeNameText != null) chargeNameText.text = charge != null ? charge.chargeName : noChargeText;
    }

    bool chargesOpen;
    bool launchReady;
    bool rocketInFlight;

    void SetControlsLocked(bool locked)
    {
        chargesOpen = locked;
        ApplyLocks();
    }

    void ApplyLocks()
    {
        foreach (Interactable control in lockWhileChargesOpen) UpdateLock(control);
        foreach (Interactable control in lockWhenReady) UpdateLock(control);
    }

    void UpdateLock(Interactable control)
    {
        if (control == null) return;
        if (rocketInFlight && (control == coordinatesJoystick || control == crosshairJoystick))
        {
            control.IsLocked = false;
            return;
        }
        control.IsLocked = (chargesOpen && System.Array.IndexOf(lockWhileChargesOpen, control) >= 0)
                           || (launchReady && System.Array.IndexOf(lockWhenReady, control) >= 0);
    }

    void OnTaskChanged(RocketTask task)
    {
        rocketNameText.text = "Rocket: " + task.rocketName;
        if (missionTimer != null) missionTimer.Restart();
        config.currentPinCode = "";
        pinKeypad.ResetKeypad();
        OnTargetChanged(targetResolver.HasTarget, targetResolver.Coordinates);
    }

    void OnTargetChanged(bool hasTarget, Vector2 coordinates)
    {
        config.hasTarget = hasTarget;
        config.currentCoordinates = coordinates;
        coordinatesDisplay.Show(hasTarget, coordinates);
        fuelComputer.SetTarget(hasTarget, coordinates);
        flightPath.SetTarget(hasTarget, coordinates);
        crosshair.SetOnTarget(launchValidator.IsOnTarget(hasTarget, coordinates));
        RefreshIndicators();
    }

    void OnPinCodeEntered(string code)
    {
        config.currentPinCode = code;

        if (launchValidator.IsPinCodeCorrect)
        {
            pinKeypad.Accept();
        }
        else
        {
            config.currentPinCode = "";
            pinKeypad.Reject();
        }

        RefreshIndicators();
    }

    void OnLaunchPressed()
    {
        switch (rocketFlight.Launch(flightPath, config.currentCharge))
        {
            case LaunchResult.Launched:
                flightPath.SetHidden(true);
                launchButton.IsLocked = true;
                rocketInFlight = true;
                ApplyLocks();
                if (cameraDirector != null) cameraDirector.StartTracking();
                if (missionTimer != null) missionTimer.Stop();
                break;
            case LaunchResult.AlreadyFlying:
                Debug.Log("Пуск невозможен: предыдущая ракета ещё в полёте");
                break;
            case LaunchResult.NoRocketModel:
                Debug.Log(config.currentCharge == null
                    ? "Пуск невозможен: заряд не выбран"
                    : $"Пуск невозможен: у заряда {config.currentCharge.chargeName} не указан Rocket Prefab");
                break;
            case LaunchResult.NoTarget:
                Debug.Log("Пуск невозможен: прицел не наведён на планету");
                break;
        }
    }

    void OnRocketArrived()
    {
        explosion.Play(rocketFlight.ImpactPosition);
        flightPath.SetHidden(false);
        if (cameraDirector != null) cameraDirector.Impact();
    }

    void OnExplosionFinished()
    {
        rocketInFlight = false;
        ApplyLocks();
        if (cameraDirector != null) cameraDirector.StopTracking();
        launchValidator.NextMission();
        launchButton.IsLocked = false;
    }

    void RefreshIndicators()
    {
        if (rocketInFlight) return;

        coordinatesLamp.SetOn(launchValidator.IsCoordinatesCorrect);
        trajectoryLamp.SetOn(launchValidator.IsTrajectoryCorrect);
        fuelLamp.SetOn(launchValidator.IsFuelCorrect);
        chargeLamp.SetOn(launchValidator.IsChargeCorrect);
        pinCodeLamp.SetOn(launchValidator.IsPinCodeCorrect);

        bool ready = coordinatesLamp.IsOn && trajectoryLamp.IsOn && fuelLamp.IsOn && chargeLamp.IsOn && pinCodeLamp.IsOn;
        if (ready == launchReady) return;

        launchReady = ready;
        if (ready) Debug.Log("Все параметры настроены, управление заблокировано");
        ApplyLocks();
        launchCover.SetOpen(ready);
    }
}
