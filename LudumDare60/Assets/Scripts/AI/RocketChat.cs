using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class RocketChat : MonoBehaviour
{
    [SerializeField] RocketAIClient client;
    [SerializeField] LaunchValidator launchValidator;
    [SerializeField] TMP_InputField input;
    [SerializeField] TMP_Text log;
    [SerializeField] TMP_Text rocketNameText;
    [SerializeField] string rocketNamePrefix = "Rocket: ";
    [SerializeField] TMP_Text moodText;
    [SerializeField] string noMoodText = "--";
    [SerializeField] string offlineMoodText = "NO SIGNAL";
    [SerializeField] int maxHistory = 8;
    [SerializeField] int maxVisibleLines = 12;
    [SerializeField] string operatorPrefix = "> ";
    [SerializeField] string thinkingText = "...";
    [SerializeField] Color pinColor = new Color(1f, 0.15f, 0.1f);

    public event Action<RocketChatReply> Replied;
    public string RocketName => rocketName;

    readonly List<RocketChatLine> history = new List<RocketChatLine>();
    readonly List<string> visible = new List<string>();
    readonly StringBuilder builder = new StringBuilder();
    string rocketName;
    string charge;
    string mission;
    string pin;
    int contextVersion;

    void OnEnable()
    {
        if (launchValidator != null) launchValidator.TaskChanged += OnTaskChanged;
        if (input != null) input.onSubmit.AddListener(Submit);
    }

    void OnDisable()
    {
        if (launchValidator != null) launchValidator.TaskChanged -= OnTaskChanged;
        if (input != null) input.onSubmit.RemoveListener(Submit);
    }

    void Start()
    {
        if (launchValidator != null) OnTaskChanged(launchValidator.CurrentTask);
    }

    void OnTaskChanged(RocketTask task)
    {
        if (task == null) return;
        SetContext(task.rocketName, task.charge != null ? task.charge.chargeName : "", task.description, task.pinCode);
    }

    public void SetContext(string newRocketName, string newCharge, string newMission, string newPin = "")
    {
        rocketName = newRocketName;
        charge = newCharge ?? "";
        mission = newMission ?? "";
        pin = newPin ?? "";
        contextVersion++;
        history.Clear();
        visible.Clear();
        Redraw(false);

        if (rocketNameText != null) rocketNameText.text = rocketNamePrefix + rocketName;
        if (moodText != null) moodText.text = noMoodText;
    }

    public void SubmitInput() => Submit(input != null ? input.text : "");

    public void Submit(string text)
    {
        if (string.IsNullOrEmpty(rocketName) || client == null || client.IsBusy) return;

        text = text?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        if (input != null)
        {
            input.text = "";
            input.ActivateInputField();
        }

        AddVisible(operatorPrefix + text);
        Redraw(true);

        int version = contextVersion;
        string speaker = rocketName;
        client.Send(rocketName, charge, mission, pin, text, history, reply =>
        {
            if (version != contextVersion) return;

            history.Add(new RocketChatLine("operator", text));
            history.Add(new RocketChatLine("rocket", reply.reply));
            while (history.Count > maxHistory) history.RemoveAt(0);

            AddVisible($"{speaker}: {HighlightPin(reply.reply)}");
            Redraw(false);
            if (moodText != null) moodText.text = reply.offline ? offlineMoodText : (reply.mood ?? noMoodText).ToUpperInvariant();
            Replied?.Invoke(reply);
        });
    }

    string HighlightPin(string text)
    {
        if (string.IsNullOrEmpty(pin) || string.IsNullOrEmpty(text) || !text.Contains(pin)) return text;
        string hex = ColorUtility.ToHtmlStringRGB(pinColor);
        return text.Replace(pin, $"<color=#{hex}>{pin}</color>");
    }

    void AddVisible(string line)
    {
        visible.Add(line);
        while (visible.Count > maxVisibleLines) visible.RemoveAt(0);
    }

    void Redraw(bool thinking)
    {
        if (log == null) return;

        builder.Clear();
        foreach (string line in visible) builder.AppendLine(line);
        if (thinking && !string.IsNullOrEmpty(rocketName)) builder.Append(rocketName).Append(": ").Append(thinkingText);
        log.text = builder.ToString();
    }
}
