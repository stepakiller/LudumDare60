using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class LaunchCountdown : MonoBehaviour
{
    [SerializeField] TMP_Text countdownText;
    [SerializeField] GameObject[] hideDuringCountdown;
    [SerializeField] int startFrom = 3;
    [SerializeField] float stepDuration = 1f;
    [SerializeField] string finalText = "Start!";
    [SerializeField] float finalDuration = 0.8f;

    [Header("Анимация")]
    [SerializeField] float popScale = 1.6f;
    [SerializeField] float popDuration = 0.35f;
    [SerializeField] Ease popEase = Ease.OutBack;

    public event Action FinalShown;
    public event Action<int> Ticked;
    public bool IsRunning { get; private set; }

    Sequence sequence;
    bool[] wasActive;

    void Awake()
    {
        if (countdownText != null) countdownText.gameObject.SetActive(false);
    }

    public void Play(Action onFinished)
    {
        sequence?.Kill();
        IsRunning = true;
        HideMonitor();
        countdownText.gameObject.SetActive(true);

        sequence = DOTween.Sequence().SetLink(gameObject);
        for (int i = startFrom; i >= 1; i--)
        {
            string label = i.ToString();
            int number = i;
            sequence.AppendCallback(() =>
            {
                Pop(label);
                Ticked?.Invoke(number);
            });
            sequence.AppendInterval(stepDuration);
        }

        sequence.AppendCallback(() =>
        {
            Pop(finalText);
            FinalShown?.Invoke();
        });
        sequence.AppendInterval(finalDuration);
        sequence.OnComplete(() =>
        {
            Finish();
            onFinished?.Invoke();
        });
    }

    public void Cancel()
    {
        if (!IsRunning) return;
        sequence?.Kill();
        Finish();
    }

    void Pop(string label)
    {
        countdownText.text = label;
        Transform t = countdownText.transform;
        t.DOKill();
        countdownText.DOKill();
        t.localScale = Vector3.one * popScale;
        countdownText.alpha = 0f;
        t.DOScale(1f, popDuration).SetEase(popEase).SetLink(gameObject);
        countdownText.DOFade(1f, popDuration * 0.6f).SetLink(gameObject);
    }

    void HideMonitor()
    {
        if (hideDuringCountdown == null) return;
        wasActive = new bool[hideDuringCountdown.Length];
        for (int i = 0; i < hideDuringCountdown.Length; i++)
        {
            GameObject item = hideDuringCountdown[i];
            if (item == null) continue;
            wasActive[i] = item.activeSelf;
            item.SetActive(false);
        }
    }

    void RestoreMonitor()
    {
        if (hideDuringCountdown == null || wasActive == null) return;
        for (int i = 0; i < hideDuringCountdown.Length; i++)
            if (hideDuringCountdown[i] != null && wasActive[i]) hideDuringCountdown[i].SetActive(true);
    }

    void Finish()
    {
        IsRunning = false;
        countdownText.transform.DOKill();
        countdownText.DOKill();
        countdownText.gameObject.SetActive(false);
        RestoreMonitor();
    }
}
