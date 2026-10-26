using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

public class GameSounds : MonoBehaviour
{
    [Serializable]
    public class Sound
    {
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0f, 0.3f)] public float pitchVariation = 0.05f;
        [Min(0f)] public float delay;
        public AudioMixerGroup output;

        [NonSerialized] public int lastIndex = -1;
    }

    [Serializable]
    public class LightSwitch
    {
        public ToggleSwitch toggle;
        public Transform[] lights;
    }

    [Header("Общее")]
    [SerializeField] AudioMixerGroup output;
    [SerializeField] float minDistance = 0.3f;
    [SerializeField] float maxDistance = 8f;

    [Header("1. Крутилка топлива")]
    [SerializeField] RotaryKnob fuelKnob;
    [SerializeField] Sound fuelKnobSound;

    [Header("2. Кнопки выбора заряда")]
    [SerializeField] PushButton[] chargeButtons;
    [SerializeField] Sound chargeButtonSound;

    [Header("3. Рычаг траектории")]
    [SerializeField] SliderLever trajectoryLever;
    [SerializeField] Sound trajectoryLeverSound;

    [Header("4. Печать листа в факсе")]
    [SerializeField] PaperBurn paperSheet;
    [SerializeField] Transform fax;
    [SerializeField] Sound faxPrintSound;

    [Header("5. Колесико зума")]
    [SerializeField] RotaryKnob zoomKnob;
    [SerializeField] Sound zoomKnobSound;

    [Header("6. Сгорание листа (лист берётся из пункта 4)")]
    [SerializeField] Sound paperBurnSound;

    [Header("7. Тумблеры")]
    [SerializeField] ToggleSwitch[] toggles;
    [SerializeField] Sound toggleSound;

    [Header("8. Клавиши пин-кода")]
    [SerializeField] KeypadButton[] pinKeys;
    [SerializeField] Sound pinKeySound;

    [Header("9. Крышка кнопки запуска")]
    [SerializeField] LaunchCover launchCover;
    [SerializeField] Sound launchCoverOpenSound;

    [Header("10. Кнопка запуска")]
    [SerializeField] PushButton launchButton;
    [SerializeField] Sound launchButtonSound;

    [Header("Главный монитор (откуда звучат пункты 11 и 12)")]
    [SerializeField] Transform mainMonitor;

    [Header("11. Прицел навёлся на цель")]
    [SerializeField] CrosshairController crosshair;
    [SerializeField] Transform targetLockedFrom;
    [SerializeField] Sound targetLockedSound;

    [Header("12. Выбор снаряда")]
    [SerializeField] ChargeSelector chargeSelector;
    [SerializeField] Sound chargeChosenSound;

    [Header("13. Неверный и верный пин-код")]
    [SerializeField] PinCodeKeypad pinKeypad;
    [SerializeField] Sound pinWrongSound;
    [SerializeField] Sound pinCorrectSound;

    [Header("14. Свет от тумблеров")]
    [SerializeField] LightSwitch[] lightSwitches;
    [SerializeField] Sound lightOnSound;
    [SerializeField] Sound lightOffSound;

    [Header("15. Чат с ИИ")]
    [SerializeField] TMP_InputField chatInput;
    [SerializeField] RocketChat rocketChat;
    [SerializeField] Transform chatScreen;
    [SerializeField] Sound chatTypeSound;
    [SerializeField] Sound chatSendSound;
    [SerializeField] Sound chatReplySound;

    [Header("16. Пуск")]
    [SerializeField] LaunchCountdown countdown;
    [SerializeField] Transform countdownFrom;
    [SerializeField] Sound countdownBeepSound;
    [SerializeField] Sound startSound;
    [SerializeField] Transform liftOffFrom;
    [SerializeField] Sound liftOffSound;

    [Header("17. Сирена")]
    [SerializeField] Siren siren;
    [SerializeField] Sound sirenSound;
    [SerializeField] float sirenFadeOut = 0.3f;

    int typedLength;
    AudioSource source2D;
    AudioSource sirenSource;
    Tween sirenFade;

    readonly Dictionary<Transform, AudioSource> sources = new Dictionary<Transform, AudioSource>();
    readonly Dictionary<Transform, AudioSource> detachedSources = new Dictionary<Transform, AudioSource>();
    readonly List<Action> unsubscribers = new List<Action>();

    void OnEnable()
    {
        if (fuelKnob != null)
        {
            Action<int> handler = _ => Play(fuelKnobSound, fuelKnob.transform);
            fuelKnob.ValueChanged += handler;
            unsubscribers.Add(() => fuelKnob.ValueChanged -= handler);
        }

        if (chargeButtons != null)
            foreach (PushButton button in chargeButtons)
            {
                if (button == null) continue;
                Action handler = () => Play(chargeButtonSound, button.transform);
                button.Pressed += handler;
                unsubscribers.Add(() => button.Pressed -= handler);
            }

        if (trajectoryLever != null)
        {
            Action<int> handler = _ => Play(trajectoryLeverSound, trajectoryLever.transform);
            trajectoryLever.ValueChanged += handler;
            unsubscribers.Add(() => trajectoryLever.ValueChanged -= handler);
        }

        if (paperSheet != null)
        {
            Action printHandler = () => Play(faxPrintSound, fax != null ? fax : paperSheet.transform);
            Action burnHandler = () => Play(paperBurnSound, paperSheet.transform);
            paperSheet.PrintStarted += printHandler;
            paperSheet.BurnStarted += burnHandler;
            unsubscribers.Add(() => paperSheet.PrintStarted -= printHandler);
            unsubscribers.Add(() => paperSheet.BurnStarted -= burnHandler);
        }

        if (zoomKnob != null)
        {
            Action<int> handler = _ => Play(zoomKnobSound, zoomKnob.transform);
            zoomKnob.ValueChanged += handler;
            unsubscribers.Add(() => zoomKnob.ValueChanged -= handler);
        }

        if (toggles != null)
            foreach (ToggleSwitch toggle in toggles)
            {
                if (toggle == null) continue;
                Action<bool> handler = _ => Play(toggleSound, toggle.transform);
                toggle.Toggled += handler;
                unsubscribers.Add(() => toggle.Toggled -= handler);
            }

        if (pinKeys != null)
            foreach (KeypadButton key in pinKeys)
            {
                if (key == null) continue;
                Action<int> handler = _ => Play(pinKeySound, key.transform);
                key.Pressed += handler;
                unsubscribers.Add(() => key.Pressed -= handler);
            }

        if (launchCover != null)
        {
            Action<bool> handler = open =>
            {
                if (open) Play(launchCoverOpenSound, launchCover.transform);
            };
            launchCover.OpenChanged += handler;
            unsubscribers.Add(() => launchCover.OpenChanged -= handler);
        }

        if (launchButton != null)
        {
            Action handler = () => Play(launchButtonSound, launchButton.transform);
            launchButton.Pressed += handler;
            unsubscribers.Add(() => launchButton.Pressed -= handler);
        }

        SubscribeExtra();
        SubscribeChat();
        SubscribeLaunch();
    }

    void SubscribeLaunch()
    {
        if (countdown != null)
        {
            Transform beepFrom = countdownFrom != null ? countdownFrom : MonitorOr(countdown.transform);

            Action<int> tick = _ => Play(countdownBeepSound, beepFrom);
            Action final = () =>
            {
                Play(startSound, beepFrom);
                if (liftOffFrom != null) Play(liftOffSound, liftOffFrom);
                else Play2D(liftOffSound);
            };

            countdown.Ticked += tick;
            countdown.FinalShown += final;
            unsubscribers.Add(() => countdown.Ticked -= tick);
            unsubscribers.Add(() => countdown.FinalShown -= final);
        }

        if (siren != null)
        {
            Action<bool> switched = SetSiren;
            siren.Switched += switched;
            unsubscribers.Add(() => siren.Switched -= switched);
        }
    }

    void SetSiren(bool on)
    {
        if (sirenSound == null || sirenSound.clips == null || sirenSound.clips.Length == 0) return;

        if (sirenSource == null)
        {
            GameObject holder = new GameObject("Siren Loop");
            holder.transform.SetParent(transform, false);
            sirenSource = CreateSource(holder);
            sirenSource.loop = true;
        }

        sirenFade?.Kill();

        if (on)
        {
            sirenSource.transform.position = siren.transform.position;
            sirenSource.clip = PickClip(sirenSound);
            sirenSource.outputAudioMixerGroup = sirenSound.output != null ? sirenSound.output : output;
            sirenSource.volume = sirenSound.volume;
            sirenSource.pitch = 1f + UnityEngine.Random.Range(-sirenSound.pitchVariation, sirenSound.pitchVariation);
            sirenSource.Play();
            return;
        }

        if (!sirenSource.isPlaying) return;
        AudioSource source = sirenSource;
        sirenFade = DOTween.To(() => source.volume, v => source.volume = v, 0f, Mathf.Max(0.01f, sirenFadeOut))
                           .OnComplete(source.Stop)
                           .SetLink(gameObject);
    }

    void Play2D(Sound sound)
    {
        if (sound == null || sound.clips == null || sound.clips.Length == 0) return;

        if (source2D == null)
        {
            source2D = gameObject.AddComponent<AudioSource>();
            source2D.playOnAwake = false;
            source2D.spatialBlend = 0f;
            source2D.outputAudioMixerGroup = output;
        }

        if (sound.delay > 0f)
        {
            DOVirtual.DelayedCall(sound.delay, () => PlayOn(source2D, sound)).SetLink(gameObject);
            return;
        }

        PlayOn(source2D, sound);
    }

    void PlayOn(AudioSource source, Sound sound)
    {
        AudioClip clip = PickClip(sound);
        if (clip == null) return;
        source.outputAudioMixerGroup = sound.output != null ? sound.output : output;
        source.pitch = 1f + UnityEngine.Random.Range(-sound.pitchVariation, sound.pitchVariation);
        source.PlayOneShot(clip, sound.volume);
    }

    void SubscribeExtra()
    {
        if (crosshair != null)
        {
            Action<bool> handler = onTarget =>
            {
                if (onTarget) Play(targetLockedSound, targetLockedFrom != null ? targetLockedFrom : MonitorOr(crosshair.transform));
            };
            crosshair.OnTargetChanged += handler;
            unsubscribers.Add(() => crosshair.OnTargetChanged -= handler);
        }

        if (chargeSelector != null)
        {
            Action handler = () => Play(chargeChosenSound, MonitorOr(chargeSelector.transform));
            chargeSelector.Closed += handler;
            unsubscribers.Add(() => chargeSelector.Closed -= handler);
        }

        if (pinKeypad != null)
        {
            Action handler = () => Play(pinWrongSound, pinKeypad.transform);
            pinKeypad.Rejected += handler;
            unsubscribers.Add(() => pinKeypad.Rejected -= handler);

            Action correctHandler = () => Play(pinCorrectSound, pinKeypad.transform);
            pinKeypad.Accepted += correctHandler;
            unsubscribers.Add(() => pinKeypad.Accepted -= correctHandler);
        }

        if (lightSwitches != null)
            foreach (LightSwitch entry in lightSwitches)
            {
                if (entry == null || entry.toggle == null) continue;
                Transform[] lights = entry.lights;
                Action<bool> handler = on =>
                {
                    if (lights == null) return;
                    foreach (Transform light in lights)
                        if (light != null) Play(on ? lightOnSound : lightOffSound, light, true);
                };
                entry.toggle.Toggled += handler;
                ToggleSwitch toggle = entry.toggle;
                unsubscribers.Add(() => toggle.Toggled -= handler);
            }
    }

    void SubscribeChat()
    {
        Transform chatFrom = chatScreen != null ? chatScreen : chatInput != null ? chatInput.transform : null;

        if (chatInput != null)
        {
            typedLength = chatInput.text.Length;

            UnityAction<string> typed = text =>
            {
                int length = text != null ? text.Length : 0;
                bool clearedAfterSend = length == 0 && typedLength > 1;
                if (length != typedLength && !clearedAfterSend) Play(chatTypeSound, chatFrom);
                typedLength = length;
            };
            UnityAction<string> submitted = text =>
            {
                if (!string.IsNullOrWhiteSpace(text)) Play(chatSendSound, chatFrom);
            };

            chatInput.onValueChanged.AddListener(typed);
            chatInput.onSubmit.AddListener(submitted);
            unsubscribers.Add(() => chatInput.onValueChanged.RemoveListener(typed));
            unsubscribers.Add(() => chatInput.onSubmit.RemoveListener(submitted));
        }

        if (rocketChat != null)
        {
            Action<RocketChatReply> replied = _ => Play(chatReplySound, chatFrom != null ? chatFrom : rocketChat.transform);
            rocketChat.Replied += replied;
            unsubscribers.Add(() => rocketChat.Replied -= replied);
        }
    }

    Transform MonitorOr(Transform fallback) => mainMonitor != null ? mainMonitor : fallback;

    void OnDisable()
    {
        foreach (Action unsubscribe in unsubscribers) unsubscribe();
        unsubscribers.Clear();
    }

    void Play(Sound sound, Transform at, bool detached = false)
    {
        if (sound == null || sound.clips == null || sound.clips.Length == 0 || at == null) return;

        if (sound.delay > 0f)
        {
            DOVirtual.DelayedCall(sound.delay, () => PlayNow(sound, at, detached)).SetLink(gameObject);
            return;
        }

        PlayNow(sound, at, detached);
    }

    void PlayNow(Sound sound, Transform at, bool detached)
    {
        if (at == null) return;

        AudioClip clip = PickClip(sound);
        if (clip == null) return;

        AudioSource source = detached ? DetachedSourceFor(at) : SourceFor(at);
        source.outputAudioMixerGroup = sound.output != null ? sound.output : output;
        source.pitch = 1f + UnityEngine.Random.Range(-sound.pitchVariation, sound.pitchVariation);
        source.PlayOneShot(clip, sound.volume);
    }

    static AudioClip PickClip(Sound sound)
    {
        int count = sound.clips.Length;
        int index = UnityEngine.Random.Range(0, count);
        if (count > 1 && index == sound.lastIndex) index = (index + 1) % count;
        sound.lastIndex = index;
        return sound.clips[index];
    }

    AudioSource SourceFor(Transform at)
    {
        if (sources.TryGetValue(at, out AudioSource existing) && existing != null) return existing;

        AudioSource source = CreateSource(at.gameObject);
        sources[at] = source;
        return source;
    }

    AudioSource DetachedSourceFor(Transform at)
    {
        if (!detachedSources.TryGetValue(at, out AudioSource source) || source == null)
        {
            GameObject holder = new GameObject("Sound " + at.name);
            holder.transform.SetParent(transform, false);
            source = CreateSource(holder);
            detachedSources[at] = source;
        }

        source.transform.position = at.position;
        return source;
    }

    AudioSource CreateSource(GameObject owner)
    {
        AudioSource source = owner.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.dopplerLevel = 0f;
        source.outputAudioMixerGroup = output;
        return source;
    }
}
