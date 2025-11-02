using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Mixer Groups")]
    [SerializeField] private AudioMixerGroup musicMixer;
    [SerializeField] private AudioMixerGroup sfxMixer;
    [SerializeField] private AudioMixerGroup uiMixer;

    [Header("Music Source")]
    [SerializeField] private AudioSource musicSource; // single persistent source for bgm

    [Header("Pooling")]
    [SerializeField] private GameObject audioSourcePrefab;
    [SerializeField, Range(5, 50)] private int poolDefaultCapacity = 15;
    [SerializeField, Range(5, 100)] private int poolMaxSize = 40;
    private ObjectPool<AudioSource> sfxPool;

    [Header("Libraries")]
    [SerializeField] private MusicEntry[] musicLibrary;
    [SerializeField] private SfxEntry[] sfxLibrary;
    [SerializeField] private UiEntry[] uiLibrary;

    private Tween musicFadeTween;
    private AudioClip lastPlayedSfxClip;

    #region LIBRARY TYPES

    [System.Serializable]
    public class MusicEntry
    {
        public string musicID;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [System.Serializable]
    public class SfxEntry
    {
        [Tooltip("ID used from code, e.g. 'enemy_hit' or 'pickup'")]
        public string sfxID;
        [Tooltip("One is picked at random")]
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Min/Max pitch for this SFX ID")]
        public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

        public AudioClip GetRandomClip()
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }

        public float GetRandomPitch()
        {
            return Random.Range(pitchRange.x, pitchRange.y);
        }
    }

    [System.Serializable]
    public class UiEntry
    {
        public string uiID;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

        public AudioClip GetRandomClip()
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }

        public float GetRandomPitch()
        {
            return Random.Range(pitchRange.x, pitchRange.y);
        }
    }

    #endregion

    #region UNITY

    private void Awake()
    {
        // singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitMusicSource();
        InitPool();

        // optional: pick music on scene load
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        musicFadeTween?.Kill();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #endregion

    #region INIT

    private void InitMusicSource()
    {
        if (musicSource == null)
        {
            var go = new GameObject("MusicSource");
            go.transform.SetParent(transform);
            musicSource = go.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }

        if (musicMixer != null)
            musicSource.outputAudioMixerGroup = musicMixer;
    }

    private void InitPool()
    {
        sfxPool = new ObjectPool<AudioSource>(
            createFunc: CreatePooledSource,
            actionOnGet: OnGetFromPool,
            actionOnRelease: OnReturnToPool,
            actionOnDestroy: OnDestroyPooledSource,
            collectionCheck: true,
            defaultCapacity: poolDefaultCapacity,
            maxSize: poolMaxSize
        );
    }

    private AudioSource CreatePooledSource()
    {
        GameObject go = audioSourcePrefab != null
            ? Instantiate(audioSourcePrefab, transform)
            : new GameObject("PooledSFX", typeof(AudioSource));

        var src = go.GetComponent<AudioSource>();
        if (src == null)
            src = go.AddComponent<AudioSource>();

        src.playOnAwake = false;
        src.loop = false;
        // default to SFX mixer, we can override when we use it for UI
        if (sfxMixer != null)
            src.outputAudioMixerGroup = sfxMixer;

        go.SetActive(false);
        return src;
    }

    private void OnGetFromPool(AudioSource src)
    {
        src.gameObject.SetActive(true);
    }

    private void OnReturnToPool(AudioSource src)
    {
        src.Stop();
        src.clip = null;
        src.transform.localPosition = Vector3.zero;
        src.spatialBlend = 0f;
        src.gameObject.SetActive(false);
    }

    private void OnDestroyPooledSource(AudioSource src)
    {
        if (src != null)
            Destroy(src.gameObject);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // if you want automatic scene-based music, add simple logic here, e.g.:
        // if (scene.buildIndex == 0) PlayMusicByID("main_menu");
    }

    #endregion

    #region MUSIC

    public void PlayMusicByID(string musicID, bool fade = true, float fadeTime = 1f)
    {
        var entry = GetMusicEntry(musicID);
        if (entry == null || entry.clip == null) return;

        // same clip? do nothing
        if (musicSource.clip == entry.clip) return;

        musicFadeTween?.Kill();

        if (!fade)
        {
            musicSource.clip = entry.clip;
            musicSource.volume = entry.volume;
            musicSource.Play();
        }
        else
        {
            // fade out current, then fade in new
            float prevVol = musicSource.volume;

            musicFadeTween = musicSource.DOFade(0f, fadeTime).OnComplete(() =>
            {
                musicSource.clip = entry.clip;
                musicSource.volume = 0f;
                musicSource.Play();
                musicFadeTween = musicSource.DOFade(entry.volume, fadeTime);
            });
        }
    }

    public void StopMusic(bool fade = true, float fadeTime = 0.5f)
    {
        musicFadeTween?.Kill();

        if (!musicSource.isPlaying)
            return;

        if (!fade)
        {
            musicSource.Stop();
            return;
        }

        float cached = musicSource.volume;
        musicFadeTween = musicSource.DOFade(0f, fadeTime).OnComplete(() =>
        {
            musicSource.Stop();
            musicSource.volume = cached;
        });
    }

    private MusicEntry GetMusicEntry(string id)
    {
        if (string.IsNullOrEmpty(id) || musicLibrary == null) return null;
        for (int i = 0; i < musicLibrary.Length; i++)
        {
            if (musicLibrary[i] != null && musicLibrary[i].musicID == id)
                return musicLibrary[i];
        }
        Debug.LogWarning($"[AudioManager] Music ID '{id}' not found.");
        return null;
    }

    #endregion

    #region SFX

    /// <summary>
    /// 2D SFX, from SFX library, uses SFX mixer, uses pool
    /// </summary>
    public void PlaySFX(string sfxID, float volumeMul = 1f)
    {
        var entry = GetSfxEntry(sfxID);
        if (entry == null) return;

        var clip = entry.GetRandomClip();
        if (clip == null) return;

        // optional antispam
        if (clip == lastPlayedSfxClip) { /*comment this out if you don't want it*/ }
        lastPlayedSfxClip = clip;

        var src = sfxPool.Get();
        src.outputAudioMixerGroup = sfxMixer; // ensure correct mixer
        src.clip = clip;
        src.volume = entry.volume * volumeMul;
        src.pitch = entry.GetRandomPitch();
        src.spatialBlend = 0f;
        src.Play();

        StartCoroutine(ReturnAfter(src, clip.length));
        StartCoroutine(ResetLastPlayed(clip.length));
    }

    /// <summary>
    /// 3D SFX at world position
    /// </summary>
    public void PlaySFXAtPosition(string sfxID, Vector3 position, float volumeMul = 1f)
    {
        var entry = GetSfxEntry(sfxID);
        if (entry == null) return;

        var clip = entry.GetRandomClip();
        if (clip == null) return;

        var src = sfxPool.Get();
        src.outputAudioMixerGroup = sfxMixer;
        src.transform.position = position;
        src.clip = clip;
        src.volume = entry.volume * volumeMul;
        src.pitch = entry.GetRandomPitch();
        src.spatialBlend = 1f;
        src.Play();

        StartCoroutine(ReturnAfter(src, clip.length));
    }

    private SfxEntry GetSfxEntry(string id)
    {
        if (string.IsNullOrEmpty(id) || sfxLibrary == null) return null;
        for (int i = 0; i < sfxLibrary.Length; i++)
        {
            if (sfxLibrary[i] != null && sfxLibrary[i].sfxID == id)
                return sfxLibrary[i];
        }
        Debug.LogWarning($"[AudioManager] SFX ID '{id}' not found.");
        return null;
    }

    #endregion

    #region UI

    /// <summary>
    /// UI blips, from UI library, uses UI mixer, no pooling (can also reuse pool)
    /// </summary>
    public void PlayUI(string uiID, float volumeMul = 1f)
    {
        var entry = GetUiEntry(uiID);
        if (entry == null) return;

        var clip = entry.GetRandomClip();
        if (clip == null) return;

        // we can just grab from pool too so everything is consistent
        var src = sfxPool.Get();
        src.outputAudioMixerGroup = uiMixer; // force UI mixer
        src.clip = clip;
        src.volume = entry.volume * volumeMul;
        src.pitch = entry.GetRandomPitch();
        src.spatialBlend = 0f;
        src.Play();

        StartCoroutine(ReturnAfter(src, clip.length));
    }

    private UiEntry GetUiEntry(string id)
    {
        if (string.IsNullOrEmpty(id) || uiLibrary == null) return null;
        for (int i = 0; i < uiLibrary.Length; i++)
        {
            if (uiLibrary[i] != null && uiLibrary[i].uiID == id)
                return uiLibrary[i];
        }
        Debug.LogWarning($"[AudioManager] UI ID '{id}' not found.");
        return null;
    }

    #endregion

    #region HELPERS

    private IEnumerator ReturnAfter(AudioSource src, float time)
    {
        yield return new WaitForSeconds(time);
        if (src != null)
            sfxPool.Release(src);
    }

    private IEnumerator ResetLastPlayed(float time)
    {
        yield return new WaitForSeconds(time);
        lastPlayedSfxClip = null;
    }

    #endregion
}

