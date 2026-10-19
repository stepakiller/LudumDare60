using System;
using System.Collections.Generic;
using UnityEngine;

public class ChargeSelector : MonoBehaviour
{
    [SerializeField] ChargeData[] charges;
    [SerializeField] bool firstAtBottom = true;
    [SerializeField] bool startFromMiddle = true;

    public event Action<int> Opened;
    public event Action Closed;
    public event Action<int> HighlightChanged;
    public event Action<ChargeData> ChargeChosen;

    public IReadOnlyList<ChargeData> Charges => charges;
    public bool FirstAtBottom => firstAtBottom;
    public bool IsOpen { get; private set; }
    public int HighlightIndex { get; private set; }
    public int SelectedIndex { get; private set; } = -1;
    public ChargeData Selected => SelectedIndex >= 0 ? charges[SelectedIndex] : null;

    public void PressUp()
    {
        if (!IsOpen) { Open(); return; }
        Move(firstAtBottom ? 1 : -1);
    }

    public void PressDown()
    {
        if (!IsOpen) { Open(); return; }
        Move(firstAtBottom ? -1 : 1);
    }

    public void PressConfirm()
    {
        if (!IsOpen) { Open(); return; }

        if (HighlightIndex != SelectedIndex)
        {
            SelectedIndex = HighlightIndex;
            ChargeChosen?.Invoke(Selected);
        }

        IsOpen = false;
        Closed?.Invoke();
    }

    void Open()
    {
        if (charges == null || charges.Length == 0) return;

        HighlightIndex = SelectedIndex >= 0 ? SelectedIndex : StartIndex;
        IsOpen = true;
        Opened?.Invoke(HighlightIndex);
    }

    int StartIndex => startFromMiddle ? (charges.Length - 1) / 2 : 0;

    void Move(int direction)
    {
        int next = Mathf.Clamp(HighlightIndex + direction, 0, charges.Length - 1);
        if (next == HighlightIndex) return;

        HighlightIndex = next;
        HighlightChanged?.Invoke(HighlightIndex);
    }
}
