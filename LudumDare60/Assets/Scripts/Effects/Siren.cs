using System;
using UnityEngine;

public class Siren : MonoBehaviour
{
    [SerializeField] Transform pivot;
    [SerializeField] GameObject root;
    [SerializeField] float degreesPerSecond = 540f;
    [SerializeField] Vector3 axis = Vector3.up;
    [SerializeField] AudioSource sound;

    public event Action<bool> Switched;
    public bool IsOn { get; private set; }

    void Awake()
    {
        if (root == null && pivot != null) root = pivot.gameObject;
        Apply(false);
    }

    public void SetOn(bool on)
    {
        if (on == IsOn) return;
        Apply(on);
    }

    void Apply(bool on)
    {
        IsOn = on;
        if (root != null) root.SetActive(on);
        Switched?.Invoke(on);
        if (sound == null) return;
        if (on) sound.Play();
        else sound.Stop();
    }

    void Update()
    {
        if (IsOn && pivot != null) pivot.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
