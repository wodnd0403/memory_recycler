using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MemoryAudio3D : MonoBehaviour
{
    public static MemoryAudio3D Instance { get; private set; }

    private AudioSource source;
    private AudioClip memoryFound;
    private AudioClip memoryRestored;
    private AudioClip decision;
    private AudioClip lore;
    private AudioClip archiveOpen;
    private AudioClip archiveDenied;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0.55f;

        memoryFound = CreateTone("MR3D_MemoryFound", 0.42f, 440f, 660f, 0.18f);
        memoryRestored = CreateTone("MR3D_MemoryRestored", 0.8f, 330f, 880f, 0.25f);
        decision = CreateTone("MR3D_Decision", 0.36f, 240f, 180f, 0.20f);
        lore = CreateTone("MR3D_Lore", 0.55f, 190f, 320f, 0.16f);
        archiveOpen = CreateTone("MR3D_ArchiveOpen", 1.1f, 140f, 620f, 0.28f);
        archiveDenied = CreateTone("MR3D_ArchiveDenied", 0.45f, 160f, 110f, 0.22f);
    }

    public void PlayMemoryFound()
    {
        Play(memoryFound, 0.55f);
    }

    public void PlayMemoryRestored()
    {
        Play(memoryRestored, 0.68f);
    }

    public void PlayDecision()
    {
        Play(decision, 0.55f);
    }

    public void PlayLore()
    {
        Play(lore, 0.45f);
    }

    public void PlayArchiveOpen()
    {
        Play(archiveOpen, 0.72f);
    }

    public void PlayArchiveDenied()
    {
        Play(archiveDenied, 0.60f);
    }

    private void Play(AudioClip clip, float volume)
    {
        if (clip == null || source == null)
            return;

        source.PlayOneShot(clip, volume);
    }

    private static AudioClip CreateTone(string name, float duration, float startFrequency, float endFrequency, float amplitude)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float phase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            float frequency = Mathf.Lerp(startFrequency, endFrequency, Smooth(t));
            phase += frequency * Mathf.PI * 2f / sampleRate;

            float envelope = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            float shimmer = Mathf.Sin(phase * 2.01f) * 0.22f;
            samples[i] = (Mathf.Sin(phase) + shimmer) * envelope * amplitude;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static float Smooth(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
