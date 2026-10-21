using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class AIChatDemoBuilder
{
    const string ScenePath = "Assets/Scenes/AIChatDemo.unity";
    static readonly Color Green = new Color(0.2f, 1f, 0.3f);
    static readonly Color DarkGreen = new Color(0.02f, 0.12f, 0.04f);
    static readonly Color PanelGreen = new Color(0.04f, 0.2f, 0.07f);

    [MenuItem("Tools/LudumDare60/Create AI Chat Demo Scene")]
    static void Create()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform root = canvasGo.GetComponent<RectTransform>();

        RectTransform screen = Panel("Screen", root, DarkGreen);
        Stretch(screen, 80f, 80f, 60f, 60f);

        TMP_Text title = Text("Title", screen, "ROCKET LINK // AI CHANNEL", 44, TextAlignmentOptions.TopLeft);
        Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -90f), new Vector2(-40f, -30f));

        TMP_Text rocketLabel = Text("RocketInfo", screen, "ROCKET: --", 28, TextAlignmentOptions.TopLeft);
        Anchor(rocketLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -190f), new Vector2(-40f, -100f));

        TMP_Text moodLabel = Text("Mood", screen, "MOOD: --", 28, TextAlignmentOptions.TopRight);
        Anchor(moodLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -90f), new Vector2(-40f, -30f));

        RectTransform logPanel = Panel("LogPanel", screen, PanelGreen);
        Anchor(logPanel, Vector2.zero, Vector2.one, new Vector2(40f, 140f), new Vector2(-40f, -200f));
        TMP_Text log = Text("Log", logPanel, "", 30, TextAlignmentOptions.BottomLeft);
        Anchor(log.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 15f), new Vector2(-20f, -15f));
        log.textWrappingMode = TextWrappingModes.Normal;
        log.overflowMode = TextOverflowModes.Truncate;

        TMP_DefaultControls.Resources resources = new TMP_DefaultControls.Resources();

        GameObject inputGo = TMP_DefaultControls.CreateInputField(resources);
        inputGo.name = "Input";
        inputGo.transform.SetParent(screen, false);
        Anchor((RectTransform)inputGo.transform, Vector2.zero, new Vector2(1f, 0f), new Vector2(40f, 40f), new Vector2(-260f, 110f));
        TMP_InputField input = inputGo.GetComponent<TMP_InputField>();
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 300;
        inputGo.GetComponent<Image>().color = PanelGreen;
        StyleText(input.textComponent, 30);
        if (input.placeholder is TMP_Text placeholder)
        {
            StyleText(placeholder, 30);
            placeholder.text = "Type a message to the rocket and press Enter...";
            placeholder.color = new Color(Green.r, Green.g, Green.b, 0.4f);
        }
        input.caretColor = Green;
        input.customCaretColor = true;

        GameObject sendGo = Button("Send", screen, "SEND", resources);
        Anchor((RectTransform)sendGo.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-240f, 40f), new Vector2(-40f, 110f));

        GameObject logic = new GameObject("AIChat");
        RocketAIClient client = logic.AddComponent<RocketAIClient>();
        RocketChat chat = logic.AddComponent<RocketChat>();
        MissionGenerator generator = logic.AddComponent<MissionGenerator>();
        RocketChatDemo demo = logic.AddComponent<RocketChatDemo>();

        SerializedObject chatSo = new SerializedObject(chat);
        chatSo.FindProperty("client").objectReferenceValue = client;
        chatSo.FindProperty("input").objectReferenceValue = input;
        chatSo.FindProperty("log").objectReferenceValue = log;
        chatSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject demoSo = new SerializedObject(demo);
        demoSo.FindProperty("chat").objectReferenceValue = chat;
        demoSo.FindProperty("generator").objectReferenceValue = generator;
        demoSo.FindProperty("rocketLabel").objectReferenceValue = rocketLabel;
        demoSo.FindProperty("moodLabel").objectReferenceValue = moodLabel;
        demoSo.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddPersistentListener(sendGo.GetComponent<Button>().onClick, new UnityAction(chat.SubmitInput));

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);
        Selection.activeGameObject = logic;
        EditorGUIUtility.PingObject(logic);
    }

    static RectTransform Panel(string name, RectTransform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return (RectTransform)go.transform;
    }

    static TMP_Text Text(string name, RectTransform parent, string value, float size, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.alignment = alignment;
        StyleText(text, size);
        return text;
    }

    static GameObject Button(string name, RectTransform parent, string label, TMP_DefaultControls.Resources resources)
    {
        GameObject go = TMP_DefaultControls.CreateButton(resources);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = PanelGreen;
        TMP_Text text = go.GetComponentInChildren<TMP_Text>();
        text.text = label;
        StyleText(text, 28);
        text.alignment = TextAlignmentOptions.Center;
        return go;
    }

    static void StyleText(TMP_Text text, float size)
    {
        text.fontSize = size;
        text.color = Green;
    }

    static void Stretch(RectTransform rect, float left, float right, float bottom, float top)
    {
        Anchor(rect, Vector2.zero, Vector2.one, new Vector2(left, bottom), new Vector2(-right, -top));
    }

    static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
