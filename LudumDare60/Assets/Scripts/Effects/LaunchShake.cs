using System;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

public class LaunchShake : MonoBehaviour
{
    [SerializeField] CinemachineImpulseSource impulseSource;
    [SerializeField] CinemachineExternalImpulseListener playerListener;
    [SerializeField] int channel = 2;
    [SerializeField] float strength = 1f;

    Tween finishTween;

    void Awake()
    {
        if (impulseSource != null)
        {
            impulseSource.ImpulseDefinition.ImpulseChannel = channel;
            impulseSource.ImpulseDefinition.ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
        }

        if (playerListener != null)
        {
            playerListener.ChannelMask = channel;
            playerListener.Gain = 0f;
        }
    }

    public float Duration
    {
        get
        {
            float impulse = impulseSource != null ? impulseSource.ImpulseDefinition.ImpulseDuration : 0f;
            float reaction = playerListener != null ? playerListener.ReactionSettings.Duration : 0f;
            return Mathf.Max(impulse, reaction);
        }
    }

    public void Play(Action onFinished = null)
    {
        if (impulseSource != null) impulseSource.GenerateImpulseWithVelocity(Vector3.up * Mathf.Max(0.05f, strength));

        finishTween?.Kill();
        if (onFinished == null) return;
        finishTween = DOVirtual.DelayedCall(Duration, () => onFinished()).SetLink(gameObject);
    }
}
