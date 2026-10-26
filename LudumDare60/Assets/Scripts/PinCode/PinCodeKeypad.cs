using System;
using System.Text;
using DG.Tweening;
using UnityEngine;

public class PinCodeKeypad : MonoBehaviour
{
    [SerializeField] KeypadButton[] buttons;
    [SerializeField] int codeLength = 5;
    [SerializeField] float rejectDelay = 0.35f;

    public event Action<string> CodeEntered;
    public event Action Accepted;
    public event Action Rejected;
    public bool IsAccepted { get; private set; }

    readonly StringBuilder input = new StringBuilder();
    bool busy;
    Tween rejectTween;

    void OnEnable()
    {
        foreach (KeypadButton button in buttons) button.Pressed += OnDigitPressed;
    }

    void OnDisable()
    {
        foreach (KeypadButton button in buttons) button.Pressed -= OnDigitPressed;
    }

    void OnDigitPressed(int digit)
    {
        if (busy || IsAccepted) return;

        KeypadButton button = FindButton(digit);
        if (button != null) button.SetHeld(true);

        input.Append(digit);
        if (input.Length < codeLength) return;

        busy = true;
        CodeEntered?.Invoke(input.ToString());
    }

    public void Accept()
    {
        busy = false;
        IsAccepted = true;
        Accepted?.Invoke();
    }

    public void Reject()
    {
        rejectTween?.Kill();
        rejectTween = DOVirtual.DelayedCall(rejectDelay, ResetKeypad).SetLink(gameObject);
        Rejected?.Invoke();
    }

    public void ResetKeypad()
    {
        rejectTween?.Kill();
        input.Clear();
        busy = false;
        IsAccepted = false;
        foreach (KeypadButton button in buttons) button.SetHeld(false);
    }

    KeypadButton FindButton(int digit)
    {
        foreach (KeypadButton button in buttons)
            if (button.Digit == digit) return button;
        return null;
    }
}
