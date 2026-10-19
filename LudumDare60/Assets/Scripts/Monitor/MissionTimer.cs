using System;
using TMPro;
using UnityEngine;

public class MissionTimer : MonoBehaviour
{
    [SerializeField] TMP_Text timerText;
    [SerializeField] float duration = 600f;

    [Header("Цвета")]
    [SerializeField] Color safeColor = new Color(0.2f, 1f, 0.2f);
    [SerializeField] Color warningColor = new Color(1f, 0.92f, 0.1f);
    [SerializeField] Color dangerColor = new Color(1f, 0.55f, 0f);
    [SerializeField] Color criticalColor = new Color(1f, 0.15f, 0.1f);

    [Header("Пороги (доля оставшегося времени)")]
    [SerializeField, Range(0f, 1f)] float warningAt = 0.5f;
    [SerializeField, Range(0f, 1f)] float dangerAt = 0.25f;
    [SerializeField, Range(0f, 1f)] float criticalAt = 0.1f;

    public event Action Expired;
    public float Remaining { get; private set; }
    public bool IsRunning { get; private set; }

    int shownSeconds = -1;

    public void Restart()
    {
        Remaining = duration;
        IsRunning = true;
        shownSeconds = -1;
        UpdateView();
    }

    public void Stop()
    {
        IsRunning = false;
    }

    void Update()
    {
        if (!IsRunning) return;

        Remaining -= Time.deltaTime;
        if (Remaining <= 0f)
        {
            Remaining = 0f;
            IsRunning = false;
            UpdateView();
            Expired?.Invoke();
            return;
        }

        UpdateView();
    }

    void UpdateView()
    {
        int seconds = Mathf.CeilToInt(Remaining);
        if (seconds == shownSeconds) return;
        shownSeconds = seconds;

        timerText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        timerText.color = ColorFor(duration > 0f ? Remaining / duration : 0f);
    }

    Color ColorFor(float fraction)
    {
        if (fraction <= criticalAt) return criticalColor;
        if (fraction <= dangerAt) return dangerColor;
        if (fraction <= warningAt) return warningColor;
        return safeColor;
    }
}
