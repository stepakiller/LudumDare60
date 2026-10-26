using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class HoverHintBuilder
{
    [MenuItem("Tools/LudumDare60/Create Hover Hint UI")]
    static void Create()
    {
        GameObject canvasObject = new GameObject("Hover Hint Canvas", typeof(Canvas), typeof(CanvasScaler));
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Hover Hint UI");

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panelObject = new GameObject("Hint Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panelObject.transform.SetParent(canvasObject.transform, false);

        RectTransform panel = panelObject.GetComponent<RectTransform>();
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.zero;
        panel.pivot = new Vector2(0f, 1f);

        Image background = panelObject.GetComponent<Image>();
        background.color = new Color(0.02f, 0.06f, 0.03f, 0.88f);
        background.raycastTarget = false;

        Outline outline = panelObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.25f, 1f, 0.35f, 0.6f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        VerticalLayoutGroup layout = panelObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 10, 12);
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panelObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text title = CreateText(panel, "Title", "Title", 22f, FontStyles.Bold, new Color(0.45f, 1f, 0.5f), -1f);
        TMP_Text description = CreateText(panel, "Description", "Description", 17f, FontStyles.Normal, new Color(0.8f, 0.95f, 0.8f), 340f);

        HoverHintUI ui = canvasObject.AddComponent<HoverHintUI>();
        SerializedObject serialized = new SerializedObject(ui);
        serialized.FindProperty("interactor").objectReferenceValue = Object.FindFirstObjectByType<PlayerInteractor>();
        serialized.FindProperty("panel").objectReferenceValue = panel;
        serialized.FindProperty("group").objectReferenceValue = panelObject.GetComponent<CanvasGroup>();
        serialized.FindProperty("titleText").objectReferenceValue = title;
        serialized.FindProperty("descriptionText").objectReferenceValue = description;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        panelObject.GetComponent<CanvasGroup>().alpha = 0f;
        Selection.activeGameObject = canvasObject;
    }

    static TMP_Text CreateText(RectTransform parent, string name, string text, float size, FontStyles style, Color color, float width)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.color = color;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;

        LayoutElement element = textObject.GetComponent<LayoutElement>();
        element.preferredWidth = width;
        return label;
    }
}
