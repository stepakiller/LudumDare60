using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class RocketChatLine
{
    public string role;
    public string text;

    public RocketChatLine(string role, string text)
    {
        this.role = role;
        this.text = text;
    }
}

[Serializable]
public class RocketChatRequest
{
    public string rocket;
    public string charge;
    public string mission;
    public string pin;
    public string message;
    public int turn;
    public RocketChatLine[] history;
}

[Serializable]
public class RocketChatReply
{
    public string reply;
    public string mood;
    public bool revealed;
    public bool offline;
}

public class RocketAIClient : MonoBehaviour
{
    [SerializeField] string serverUrl = "https://your-domain.ngrok-free.app";
    [SerializeField] string apiKey = "change-me";
    [SerializeField] int timeoutSeconds = 20;

    [Header("Ответы без сервера")]
    [SerializeField, TextArea(1, 3)] string[] offlineReplies =
    {
        "...static... Link unstable. Awaiting launch codes.",
        "Signal lost. I will follow protocol, operator.",
        "Transmission corrupted. Repeat when ready.",
        "...I hear you. Barely."
    };

    public bool IsBusy { get; private set; }

    public void Send(string rocket, string charge, string mission, string pin, string message, IReadOnlyList<RocketChatLine> history, Action<RocketChatReply> onReply, int turn = 0)
    {
        if (IsBusy) return;

        RocketChatRequest request = new RocketChatRequest
        {
            rocket = rocket,
            charge = charge ?? "",
            mission = mission ?? "",
            pin = pin ?? "",
            message = message,
            turn = turn,
            history = CopyHistory(history)
        };

        StartCoroutine(SendRoutine(request, onReply));
    }

    static RocketChatLine[] CopyHistory(IReadOnlyList<RocketChatLine> history)
    {
        if (history == null || history.Count == 0) return Array.Empty<RocketChatLine>();
        RocketChatLine[] copy = new RocketChatLine[history.Count];
        for (int i = 0; i < copy.Length; i++) copy[i] = history[i];
        return copy;
    }

    IEnumerator SendRoutine(RocketChatRequest request, Action<RocketChatReply> onReply)
    {
        IsBusy = true;
        byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(request));

        using (UnityWebRequest web = new UnityWebRequest(serverUrl.TrimEnd('/') + "/chat", UnityWebRequest.kHttpVerbPOST))
        {
            web.uploadHandler = new UploadHandlerRaw(body);
            web.downloadHandler = new DownloadHandlerBuffer();
            web.timeout = timeoutSeconds;
            web.SetRequestHeader("Content-Type", "application/json");
            web.SetRequestHeader("X-Api-Key", apiKey);
            web.SetRequestHeader("ngrok-skip-browser-warning", "true");

            yield return web.SendWebRequest();

            RocketChatReply reply = null;
            if (web.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    reply = JsonUtility.FromJson<RocketChatReply>(web.downloadHandler.text);
                }
                catch (ArgumentException)
                {
                    reply = null;
                }
            }

            if (reply == null || string.IsNullOrWhiteSpace(reply.reply)) reply = OfflineReply();

            IsBusy = false;
            onReply?.Invoke(reply);
        }
    }

    RocketChatReply OfflineReply()
    {
        string text = offlineReplies != null && offlineReplies.Length > 0
            ? offlineReplies[UnityEngine.Random.Range(0, offlineReplies.Length)]
            : "...";
        return new RocketChatReply { reply = text, mood = "confused", offline = true };
    }
}
