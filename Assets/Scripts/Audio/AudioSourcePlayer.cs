using UnityEngine;

public class AudioSourcePlayer : MonoBehaviour
{
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void PlayClip(AudioClip clip, float volume = 1f, bool loop = false)
    {
        if (clip == null) return;
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.loop = loop;
        audioSource.Play();

        if (!loop)
            Destroy(gameObject, clip.length);
    }

    public void Stop()
    {
        if (audioSource.isPlaying)
            audioSource.Stop();
    }
}