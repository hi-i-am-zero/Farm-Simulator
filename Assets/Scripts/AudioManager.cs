using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Serializable] public struct Sound { public string name; public AudioClip clip; }
    [SerializeField] Sound[] sounds;
    [SerializeField] AudioSource sfxSource;

    void Awake() => Instance = this;

    public void Play(string soundName)
    {
        if (sfxSource == null) return;
        foreach (var s in sounds)
            if (s.name == soundName && s.clip != null)
            {
                sfxSource.PlayOneShot(s.clip);
                return;
            }
    }
}
