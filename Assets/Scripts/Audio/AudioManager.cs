using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Audio;
using System;

[Serializable]
public class AudioEntry
{
    public AudioTrigger id;
    public List<AudioClip> clips;

    public AudioEntry(AudioTrigger trigger, List<AudioClip> audioClips)
    {
        id = trigger;
        clips = audioClips;
    }
}

public class AudioManager : MonoBehaviour
{
    [Header("AUDIO CLIPS")]
    [SerializeField] private SO_AudioClips audioLibrary;
    [SerializeField] private AudioSource audioSourcePrefab;

    [Header("AUDIO MIXERS")]
    [SerializeField] private AudioMixerGroup masterMixer;
    [SerializeField] private AudioMixerGroup musicMixer;
    [SerializeField] private AudioMixerGroup sfxMixer;
    [SerializeField] private AudioMixerGroup uiMixer;
    [SerializeField] private AudioMixerGroup ambientMixer;

    private EventBinding<AudioEvent> audioEvent;

    private void OnEnable()
    {
        audioEvent = new EventBinding<AudioEvent>(OnAudioEvent);
        EventBus<AudioEvent>.Register(audioEvent);
    }

    private void OnDisable()
    {
        EventBus<AudioEvent>.Deregister(audioEvent);
    }

    private void OnAudioEvent(AudioEvent evt)
    {
        PlaySound(evt.id);
    }

    public void PlaySound(AudioTrigger id)
    {
        if (audioLibrary == null)
        {
            Debug.LogWarning("AudioManager: No audioLibrary assigned.");
            return;
        }

        // Determine sound type from enum name
        string mixerType = id.ToString().Split('_')[0].ToLower();
        List<AudioEntry> searchList = null;
        AudioMixerGroup mixerGroup = sfxMixer; // default

        switch (mixerType)
        {
            case "music":
                searchList = audioLibrary.musicSounds;
                mixerGroup = musicMixer;
                break;
            case "ui":
                searchList = audioLibrary.uiSounds;
                mixerGroup = uiMixer;
                break;
            case "ambient":
                searchList = audioLibrary.ambientSounds;
                mixerGroup = ambientMixer;
                break;
            case "sfx":
            default:
                searchList = audioLibrary.sfxSounds;
                mixerGroup = sfxMixer;
                break;
        }

        // Find the entry for this trigger
        AudioEntry foundEntry = null;
        if (searchList != null)
        {
            foreach (var entry in searchList)
            {
                if (entry != null && entry.id == id && entry.clips != null && entry.clips.Count > 0)
                {
                    foundEntry = entry;
                    break;
                }
            }
        }

        if (foundEntry != null)
        {
            var clip = foundEntry.clips[UnityEngine.Random.Range(0, foundEntry.clips.Count)];
            var source = Instantiate(audioSourcePrefab, transform);
            source.outputAudioMixerGroup = mixerGroup;
            source.clip = clip;
            source.Play();
            Destroy(source.gameObject, clip.length);
        }
        else
        {
            Debug.LogWarning($"AudioManager: No clip(s) found for id '{id}' in {mixerType} list.");
        }
    }
}

public enum AudioTrigger
{
    //UI
    UI_Click,
    UI_Hover,
    UI_ExitHover,
    UI_Show,
    UI_Hide,

    //SFX
    SFX_BasicObjectClick,
    SFX_BasicObjectHover,
    SFX_BasicObjectUnhover,

    //MUSIC
    Music_Theme,
    Music_Menu
}


