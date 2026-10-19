using System;
using UnityEngine;

public class MissionData : ScriptableObject
{
    public RocketTask[] rockets;
}

[Serializable]
public class RocketTask
{
    public string rocketName = "Mary";
    [TextArea(2, 5)] public string description;
    [Min(0f)] public float timeLimit = 600f;

    [Header("Цель")]
    [Range(-90f, 90f)] public float targetLatitude;
    [Range(-180f, 180f)] public float targetLongitude;
    [Min(0.01f)] public float coordinatesTolerance = 1f;

    [Header("Траектория")]
    public int trajectory = 45;

    [Header("Заряд")]
    public ChargeData charge;

    [Header("Пин-код")]
    public string pinCode = "12345";

    public Vector2 TargetCoordinates => new Vector2(targetLongitude, targetLatitude);
}
