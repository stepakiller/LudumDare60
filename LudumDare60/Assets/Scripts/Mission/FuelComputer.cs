using System;
using TMPro;
using UnityEngine;

public class FuelComputer : MonoBehaviour
{
    [Header("База")]
    [SerializeField] Transform planet;
    [SerializeField] Transform baseMarker;

    [Header("Расчёт")]
    [SerializeField] int maxFuel = 100;
    [SerializeField] int fuelStep = 10;
    [SerializeField] int minFuel = 10;

    [Header("Экран")]
    [SerializeField] TMP_Text requiredFuelText;
    [SerializeField] string noTargetText = "--";

    public event Action<bool> FuelCorrectChanged;
    public Vector2 BaseCoordinates { get; private set; }
    public bool HasTarget { get; private set; }
    public int RequiredFuel { get; private set; }
    public int CurrentFuel { get; private set; }
    public bool IsFuelCorrect => HasTarget && CurrentFuel == RequiredFuel;

    bool wasCorrect;
    int shownFuel = int.MinValue;

    void Awake()
    {
        BaseCoordinates = GeoMath.FromDirection(planet.InverseTransformPoint(baseMarker.position));
        UpdateScreen();
    }

    public void SetTarget(bool hasTarget, Vector2 coordinates)
    {
        HasTarget = hasTarget;
        RequiredFuel = hasTarget ? CalculateFuel(coordinates) : 0;
        UpdateScreen();
        CheckFuel();
    }

    public void SetCurrentFuel(int fuel)
    {
        CurrentFuel = fuel;
        CheckFuel();
    }

    public int CalculateFuel(Vector2 target)
    {
        float distance = GeoMath.AngularDistance(BaseCoordinates, target);
        float fuel = distance / 180f * maxFuel;
        int rounded = Mathf.CeilToInt(fuel / fuelStep) * fuelStep;
        return Mathf.Clamp(rounded, minFuel, maxFuel);
    }

    void UpdateScreen()
    {
        if (requiredFuelText == null) return;

        int toShow = HasTarget ? RequiredFuel : -1;
        if (toShow == shownFuel) return;
        shownFuel = toShow;

        if (HasTarget) requiredFuelText.SetText("{0}%", RequiredFuel);
        else requiredFuelText.text = noTargetText;
    }

    void CheckFuel()
    {
        bool correct = IsFuelCorrect;
        if (correct == wasCorrect) return;
        wasCorrect = correct;

        FuelCorrectChanged?.Invoke(correct);
    }
}
