using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

[DefaultExecutionOrder(-200)]
public class PauseMenu : MonoBehaviour
{
    class MenuScreen
    {
        public RectTransform Title;
        public RectTransform[] Items;
        public Vector2[] Directions;
        public float[] Offsets;
        public int[] Orders;
        public Vector2 TitleHome;
        public Vector2[] ItemHomes;
        public Sequence Sequence;
    }

    [Header("Связи")]
    [SerializeField] ChatMonitor chatMonitor;
    [SerializeField] Behaviour[] disableWhilePaused;
    [SerializeField] bool pauseAudio = true;
    [SerializeField] AudioSource[] keepPlayingOnPause;

    [Header("Затемнение")]
    [SerializeField] CanvasGroup overlay;

    [Header("Главный экран паузы")]
    [SerializeField] RectTransform title;
    [SerializeField] RectTransform[] buttons;
    [SerializeField] Button continueButton;
    [SerializeField] Button settingsButton;
    [SerializeField] Button quitButton;

    [Header("Экран настроек")]
    [SerializeField] RectTransform settingsTitle;
    [SerializeField] RectTransform[] settingsItems;
    [SerializeField] RectTransform[] settingsRightItems;

    [Header("Анимация меню")]
    [SerializeField] float titleOffset = 300f;
    [SerializeField] float buttonsOffset = 700f;
    [SerializeField] float slideInDuration = 0.45f;
    [SerializeField] float slideOutDuration = 0.3f;
    [SerializeField] float buttonsStagger = 0.06f;
    [SerializeField] float screenSwitchDelay = 0.15f;
    [SerializeField] Ease slideInEase = Ease.OutCubic;
    [SerializeField] Ease slideOutEase = Ease.InCubic;

    [Header("Размытие")]
    [SerializeField, Range(0.5f, 1.5f)] float blurRadius = 1.5f;
    [SerializeField] float blurStartDistance = 0.05f;
    [SerializeField] float blurEndDistance = 0.4f;
    [SerializeField] float volumePriority = 50f;

    [Header("Анимация затемнения")]
    [SerializeField] float fadeDuration = 0.35f;

    public event Action<bool> PauseChanged;
    public bool IsPaused { get; private set; }
    public bool InSettings { get; private set; }

    readonly List<Behaviour> controlled = new List<Behaviour>();
    readonly List<bool> wasEnabled = new List<bool>();
    Volume volume;
    VolumeProfile profile;
    float savedTimeScale = 1f;
    float weight;
    Tween tween;
    MenuScreen mainScreen;
    MenuScreen settingsScreen;

    void Awake()
    {
        CollectControlled();
        BuildVolume();

        if (keepPlayingOnPause != null)
            foreach (AudioSource source in keepPlayingOnPause)
                if (source != null) source.ignoreListenerPause = true;
        mainScreen = CreateScreen(title, buttons, null);
        settingsScreen = CreateScreen(settingsTitle, settingsItems, settingsRightItems);
        SetWeight(0f);

        if (continueButton != null) continueButton.onClick.AddListener(Resume);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
    }

    void OnDestroy()
    {
        if (IsPaused)
        {
            Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;
            AudioListener.pause = false;
        }
        if (profile != null) Destroy(profile);
    }

    void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

        if (IsPaused && InSettings)
        {
            CloseSettings();
            return;
        }

        if (!IsPaused && chatMonitor != null && chatMonitor.IsBusy) return;
        SetPaused(!IsPaused);
    }

    public void Resume() => SetPaused(false);

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OpenSettings()
    {
        if (!IsPaused || InSettings) return;
        InSettings = true;
        Animate(mainScreen, false, 0f);
        Animate(settingsScreen, true, screenSwitchDelay);
    }

    public void CloseSettings()
    {
        if (!InSettings) return;
        InSettings = false;
        Animate(settingsScreen, false, 0f);
        Animate(mainScreen, true, screenSwitchDelay);
    }

    public void SetPaused(bool paused)
    {
        if (paused == IsPaused) return;
        IsPaused = paused;

        if (paused)
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            if (pauseAudio) AudioListener.pause = true;

            wasEnabled.Clear();
            foreach (Behaviour behaviour in controlled)
            {
                wasEnabled.Add(behaviour != null && behaviour.enabled);
                if (behaviour != null) behaviour.enabled = false;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            InSettings = false;
            HideInstantly(settingsScreen);
            Animate(mainScreen, true, 0f);
        }
        else
        {
            Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;
            if (pauseAudio) AudioListener.pause = false;

            for (int i = 0; i < controlled.Count && i < wasEnabled.Count; i++)
                if (controlled[i] != null) controlled[i].enabled = wasEnabled[i];

            Animate(InSettings ? settingsScreen : mainScreen, false, 0f);
            InSettings = false;
        }

        tween?.Kill();
        tween = DOTween.To(() => weight, SetWeight, paused ? 1f : 0f, fadeDuration)
                       .SetEase(Ease.OutQuad)
                       .SetUpdate(true)
                       .SetLink(gameObject);

        PauseChanged?.Invoke(paused);
    }

    MenuScreen CreateScreen(RectTransform screenTitle, RectTransform[] leftItems, RectTransform[] rightItems)
    {
        leftItems = leftItems ?? new RectTransform[0];
        rightItems = rightItems ?? new RectTransform[0];

        int total = leftItems.Length + rightItems.Length;
        MenuScreen screen = new MenuScreen
        {
            Title = screenTitle,
            Items = new RectTransform[total],
            Directions = new Vector2[total],
            Orders = new int[total]
        };

        for (int i = 0; i < leftItems.Length; i++)
        {
            screen.Items[i] = leftItems[i];
            screen.Directions[i] = Vector2.left;
            screen.Orders[i] = i;
        }

        for (int i = 0; i < rightItems.Length; i++)
        {
            int index = leftItems.Length + i;
            screen.Items[index] = rightItems[i];
            screen.Directions[index] = Vector2.right;
            screen.Orders[index] = i;
        }

        if (screenTitle != null) screen.TitleHome = screenTitle.anchoredPosition;

        screen.ItemHomes = new Vector2[screen.Items.Length];
        screen.Offsets = new float[screen.Items.Length];
        for (int i = 0; i < screen.Items.Length; i++)
        {
            RectTransform item = screen.Items[i];
            if (item == null) continue;
            screen.ItemHomes[i] = item.anchoredPosition;
            screen.Offsets[i] = OffscreenDistance(item);
        }

        HideInstantly(screen);
        return screen;
    }

    void HideInstantly(MenuScreen screen)
    {
        screen.Sequence?.Kill();
        if (screen.Title != null) screen.Title.anchoredPosition = screen.TitleHome + Vector2.up * titleOffset;
        for (int i = 0; i < screen.Items.Length; i++)
            if (screen.Items[i] != null) screen.Items[i].anchoredPosition = screen.ItemHomes[i] + screen.Directions[i] * screen.Offsets[i];
    }

    void Animate(MenuScreen screen, bool show, float delay)
    {
        screen.Sequence?.Kill();
        Sequence sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        screen.Sequence = sequence;

        float duration = show ? slideInDuration : slideOutDuration;
        Ease ease = show ? slideInEase : slideOutEase;

        if (screen.Title != null)
        {
            Vector2 target = show ? screen.TitleHome : screen.TitleHome + Vector2.up * titleOffset;
            sequence.Insert(delay, screen.Title.DOAnchorPos(target, duration).SetEase(ease));
        }

        int maxOrder = 0;
        foreach (int value in screen.Orders) maxOrder = Mathf.Max(maxOrder, value);

        for (int i = 0; i < screen.Items.Length; i++)
        {
            if (screen.Items[i] == null) continue;
            int order = show ? screen.Orders[i] : maxOrder - screen.Orders[i];
            Vector2 target = show ? screen.ItemHomes[i] : screen.ItemHomes[i] + screen.Directions[i] * screen.Offsets[i];
            sequence.Insert(delay + order * buttonsStagger, screen.Items[i].DOAnchorPos(target, duration).SetEase(ease));
        }
    }

    float OffscreenDistance(RectTransform item)
    {
        Canvas canvas = item.GetComponentInParent<Canvas>();
        if (canvas == null) return buttonsOffset;

        RectTransform canvasRect = canvas.rootCanvas.transform as RectTransform;
        if (canvasRect == null) return buttonsOffset;

        float canvasWidth = canvasRect.rect.width;
        float itemWidth = item.rect.width * Mathf.Abs(item.localScale.x);
        return Mathf.Max(buttonsOffset, canvasWidth + itemWidth);
    }

    void SetWeight(float value)
    {
        weight = value;
        if (overlay != null)
        {
            overlay.alpha = value;
            overlay.blocksRaycasts = value > 0.01f;
        }
        volume.weight = value;
        volume.enabled = value > 0.001f;
    }

    void CollectControlled()
    {
        AddControlled(FindFirstObjectByType<PlayerInteractor>());
        if (disableWhilePaused == null) return;
        foreach (Behaviour behaviour in disableWhilePaused) AddControlled(behaviour);
    }

    void AddControlled(Behaviour behaviour)
    {
        if (behaviour != null && !controlled.Contains(behaviour)) controlled.Add(behaviour);
    }

    void BuildVolume()
    {
        GameObject volumeObject = new GameObject("Pause Blur");
        volumeObject.transform.SetParent(transform, false);
        volumeObject.layer = gameObject.layer;

        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        DepthOfField depthOfField = profile.Add<DepthOfField>();
        depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
        depthOfField.gaussianStart.Override(blurStartDistance);
        depthOfField.gaussianEnd.Override(blurEndDistance);
        depthOfField.gaussianMaxRadius.Override(blurRadius);
        depthOfField.highQualitySampling.Override(true);

        volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = volumePriority;
        volume.sharedProfile = profile;
    }
}
