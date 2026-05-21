using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class MemoryRecyclerMusicManager : MonoBehaviour
{
    public enum MusicState
    {
        None,
        Exploration,
        Night,
        Archive,
        Ending
    }

    public static MemoryRecyclerMusicManager Instance { get; private set; }

    [Header("BGM Clips")]
    public AudioClip explorationLoop;
    public AudioClip nightLoop;
    public AudioClip archiveLoop;
    public AudioClip endingLoop;

    [Header("Mix")]
    [Range(0f, 1f)] public float defaultVolume = 0.3f;
    public float fadeDuration = 1.4f;
    public bool playOnStart = true;

    private AudioSource activeSource;
    private AudioSource standbySource;
    private Coroutine transitionRoutine;
    private MusicState currentState = MusicState.None;
    private bool isNight;
    private bool inArchiveZone;
    private bool endingActive;

    public MusicState CurrentState => currentState;
    public bool IsNightMode => isNight;
    public bool IsInArchiveZone => inArchiveZone;
    public bool IsEndingActive => endingActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureSources();
    }

    private void Start()
    {
        if (WorldToneController3D.Instance != null)
            isNight = WorldToneController3D.Instance.IsNightTime;

        if (playOnStart)
            RefreshTargetMusic(true);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetNightMode(bool night)
    {
        if (isNight == night)
            return;

        isNight = night;
        RefreshTargetMusic();
    }

    public void SetArchiveZone(bool inArchive)
    {
        if (inArchiveZone == inArchive)
            return;

        inArchiveZone = inArchive;
        RefreshTargetMusic();
    }

    public void PlayEndingMusic()
    {
        if (endingActive)
            return;

        endingActive = true;
        RefreshTargetMusic();
    }

    public void ReturnToGameplayMusic()
    {
        if (!endingActive)
            return;

        endingActive = false;
        RefreshTargetMusic();
    }

    private void RefreshTargetMusic(bool instant = false)
    {
        EnsureSources();

        MusicState targetState = ResolveTargetState();
        AudioClip targetClip = GetClip(targetState);

        if (currentState == targetState && activeSource != null && activeSource.clip == targetClip)
            return;

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(TransitionTo(targetState, targetClip, instant));
    }

    private MusicState ResolveTargetState()
    {
        if (endingActive)
            return MusicState.Ending;
        if (inArchiveZone)
            return MusicState.Archive;
        if (isNight)
            return MusicState.Night;
        return MusicState.Exploration;
    }

    private AudioClip GetClip(MusicState state)
    {
        switch (state)
        {
            case MusicState.Exploration:
                return explorationLoop;
            case MusicState.Night:
                return nightLoop;
            case MusicState.Archive:
                return archiveLoop;
            case MusicState.Ending:
                return endingLoop;
            default:
                return null;
        }
    }

    private IEnumerator TransitionTo(MusicState targetState, AudioClip targetClip, bool instant)
    {
        if (targetClip == null)
        {
            float fadeOutTime = instant ? 0f : Mathf.Max(0.05f, fadeDuration);
            yield return FadeSource(activeSource, activeSource != null ? activeSource.volume : 0f, 0f, fadeOutTime);
            if (activeSource != null)
            {
                activeSource.Stop();
                activeSource.clip = null;
            }

            currentState = targetState;
            transitionRoutine = null;
            yield break;
        }

        standbySource.clip = targetClip;
        standbySource.loop = true;
        standbySource.volume = instant ? defaultVolume : 0f;
        standbySource.Play();

        if (instant)
        {
            if (activeSource != null)
                activeSource.Stop();

            SwapSources();
            activeSource.volume = defaultVolume;
            currentState = targetState;
            transitionRoutine = null;
            yield break;
        }

        float duration = Mathf.Max(0.05f, fadeDuration);
        float elapsed = 0f;
        float activeStartVolume = activeSource != null ? activeSource.volume : 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smooth = t * t * (3f - 2f * t);

            if (activeSource != null)
                activeSource.volume = Mathf.Lerp(activeStartVolume, 0f, smooth);
            standbySource.volume = Mathf.Lerp(0f, defaultVolume, smooth);
            yield return null;
        }

        if (activeSource != null)
        {
            activeSource.Stop();
            activeSource.clip = null;
            activeSource.volume = 0f;
        }

        SwapSources();
        activeSource.volume = defaultVolume;
        currentState = targetState;
        transitionRoutine = null;
    }

    private IEnumerator FadeSource(AudioSource source, float from, float to, float duration)
    {
        if (source == null)
            yield break;

        if (duration <= 0f)
        {
            source.volume = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            source.volume = Mathf.Lerp(from, to, t * t * (3f - 2f * t));
            yield return null;
        }

        source.volume = to;
    }

    private void EnsureSources()
    {
        if (activeSource != null && standbySource != null)
        {
            ConfigureSource(activeSource);
            ConfigureSource(standbySource);
            return;
        }

        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length == 0)
        {
            activeSource = gameObject.AddComponent<AudioSource>();
            standbySource = gameObject.AddComponent<AudioSource>();
        }
        else if (sources.Length == 1)
        {
            activeSource = sources[0];
            standbySource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            activeSource = sources[0];
            standbySource = sources[1];
        }

        ConfigureSource(activeSource);
        ConfigureSource(standbySource);
    }

    private void ConfigureSource(AudioSource source)
    {
        if (source == null)
            return;

        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        if (!source.isPlaying && source.clip == null)
            source.volume = 0f;
    }

    private void SwapSources()
    {
        AudioSource oldActive = activeSource;
        activeSource = standbySource;
        standbySource = oldActive;
    }
}
