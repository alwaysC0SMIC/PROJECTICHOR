using System.Collections.Generic;
using UnityEngine;
using static AudioManager;

[CreateAssetMenu(fileName = "SO_AudioClips", menuName = "ScriptableObjects/SO_AudioClips", order = 1)]
public class SO_AudioClips : ScriptableObject
{
    [Header("UI SOUNDS")]
    [SerializeField] public List<AudioEntry> uiSounds;

    [Header("SFX")]
    [SerializeField] public List<AudioEntry> sfxSounds;

    [Header("MUSIC")]
    [SerializeField] public List<AudioEntry> musicSounds;

    [Header("AMBIENT")]
    [SerializeField] public List<AudioEntry> ambientSounds;
}
