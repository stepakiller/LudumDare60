using TMPro;
using UnityEngine;

public class RocketChatDemo : MonoBehaviour
{
    [SerializeField] RocketChat chat;
    [SerializeField] MissionGenerator generator;
    [SerializeField] TMP_Text rocketLabel;
    [SerializeField] TMP_Text moodLabel;

    void OnEnable()
    {
        if (chat != null) chat.Replied += OnReplied;
    }

    void OnDisable()
    {
        if (chat != null) chat.Replied -= OnReplied;
    }

    void Start()
    {
        if (chat == null || generator == null) return;

        MissionData mission = generator.Generate(0);
        RocketTask task = mission.rockets[0];
        string charge = task.charge != null ? task.charge.chargeName : "";

        chat.SetContext(task.rocketName, charge, task.description, task.pinCode);
        if (rocketLabel != null) rocketLabel.text = $"ROCKET: {task.rocketName}\n{task.description}";
        if (moodLabel != null) moodLabel.text = "MOOD: --";

        Destroy(mission);
    }

    void OnReplied(RocketChatReply reply)
    {
        if (moodLabel == null) return;
        moodLabel.text = reply.offline ? "MOOD: NO SIGNAL" : $"MOOD: {reply.mood.ToUpperInvariant()}";
    }
}
