using System;
using System.Collections.Generic;
using UnityEngine;

public class LaunchValidator : MonoBehaviour
{
    [SerializeField] CurrentConfig config;
    [SerializeField] FuelComputer fuelComputer;
    [SerializeField] MissionGenerator generator;

    [Header("Текущее задание")]
    [SerializeField] RocketTask currentTask;

    public event Action<RocketTask> TaskChanged;
    public event Action<MissionData> MissionChanged;
    public MissionData CurrentMission => mission;
    public RocketTask CurrentTask => mission.rockets[0];
    public int MissionIndex { get; private set; }

    MissionData mission;

    void Awake()
    {
        MissionIndex = 0;
        mission = generator.Generate(MissionIndex);
        ShowTask();
    }

    void ShowTask()
    {
        currentTask = CurrentTask;
        string charge = currentTask.charge != null ? currentTask.charge.chargeName : "любой";
        Debug.Log($"Миссия {MissionIndex + 1}: ракета {currentTask.rocketName}, " +
                  $"широта {currentTask.targetLatitude:0.0}, долгота {currentTask.targetLongitude:0.0}, " +
                  $"допуск {currentTask.coordinatesTolerance:0.##}°, траектория {currentTask.trajectory}°, " +
                  $"заряд {charge}, пин-код {currentTask.pinCode}");
    }

    void OnDestroy()
    {
        if (mission != null) Destroy(mission);
    }

    public void NextMission()
    {
        if (mission != null) Destroy(mission);
        MissionIndex++;
        mission = generator.Generate(MissionIndex);
        ShowTask();
        MissionChanged?.Invoke(mission);
        TaskChanged?.Invoke(CurrentTask);
    }

    public bool IsOnTarget(bool hasTarget, Vector2 coordinates)
    {
        if (!hasTarget || mission == null || mission.rockets == null || mission.rockets.Length == 0) return false;
        RocketTask task = CurrentTask;
        return GeoMath.AngularDistance(coordinates, task.TargetCoordinates) <= task.coordinatesTolerance;
    }

    public bool IsCoordinatesCorrect => IsOnTarget(config.hasTarget, config.currentCoordinates);

    public int RequiredFuel => fuelComputer.RequiredFuel;

    public bool IsFuelCorrect => config.hasTarget && config.currentFuel == RequiredFuel;

    public bool IsTrajectoryCorrect => config.currentTrajectory == CurrentTask.trajectory;

    public bool IsChargeCorrect => config.currentCharge != null
                                   && (CurrentTask.charge == null || config.currentCharge == CurrentTask.charge);

    public bool IsPinCodeCorrect => !string.IsNullOrEmpty(config.currentPinCode)
                                    && config.currentPinCode == CurrentTask.pinCode;

    public bool Validate(out List<string> errors)
    {
        errors = new List<string>();
        RocketTask task = CurrentTask;

        if (!config.hasTarget)
        {
            errors.Add("Координаты: прицел не наведён на планету");
        }
        else
        {
            float miss = GeoMath.AngularDistance(config.currentCoordinates, task.TargetCoordinates);
            if (miss > task.coordinatesTolerance)
                errors.Add($"Координаты: промах на {miss:0.0}° (допуск {task.coordinatesTolerance:0.##}°)");
        }

        if (config.hasTarget && !IsFuelCorrect)
            errors.Add($"Топливо: выставлено {config.currentFuel}%, требуется {RequiredFuel}%");

        if (!IsTrajectoryCorrect)
            errors.Add($"Траектория: выставлено {config.currentTrajectory}°, по заданию {task.trajectory}°");

        if (config.currentCharge == null)
            errors.Add("Заряд: не выбран");
        else if (!IsChargeCorrect)
            errors.Add($"Заряд: выбран {config.currentCharge.chargeName}, по заданию {task.charge.chargeName}");

        if (!IsPinCodeCorrect)
            errors.Add("Пин-код: не введён");

        return errors.Count == 0;
    }

    [ContextMenu("Следующая миссия")]
    void NextMissionFromMenu() => NextMission();

    [ContextMenu("Проверить настройки")]
    void ValidateAndLog()
    {
        if (Validate(out List<string> errors))
            Debug.Log($"[{CurrentTask.rocketName}] Всё верно, пуск разрешён");
        else
            Debug.LogWarning($"[{CurrentTask.rocketName}] Ошибки настройки:\n" + string.Join("\n", errors));
    }
}
