using UnityEngine;
using System.Collections;

/// <summary>
/// Generates all game audio programmatically using sine waves.
/// No external audio files needed.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private AudioSource sfxSource;
    private bool muted = false;
    private bool bgMusicRunning = false;

    void Awake()
    {
        Instance = this;
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }

    public void ToggleMute()
    {
        muted = !muted;
        AudioListener.volume = muted ? 0f : 1f;
    }

    public bool IsMuted() => muted;

    // ---- SOUND EFFECTS ----

    public void PlayPickup()
    {
        if (muted) return;
        PlayTone(520f, 0.08f);
        StartCoroutine(DelayedTone(720f, 0.1f, 0.04f));
    }

    public void PlayCorrectSort()
    {
        if (muted) return;
        PlayTone(523f, 0.1f);
        StartCoroutine(DelayedTone(659f, 0.1f, 0.07f));
        StartCoroutine(DelayedTone(784f, 0.12f, 0.14f));
    }

    public void PlayWrongSort()
    {
        if (muted) return;
        PlayTone(200f, 0.2f, 0.08f, false);
        StartCoroutine(DelayedTone(150f, 0.25f, 0.08f, false));
    }

    public void PlayRepair()
    {
        if (muted) return;
        PlayTone(440f, 0.08f, 0.06f, false);
        StartCoroutine(DelayedTone(550f, 0.08f, 0.05f, false));
        StartCoroutine(DelayedTone(660f, 0.1f, 0.1f));
        StartCoroutine(DelayedTone(880f, 0.12f, 0.15f));
    }

    public void PlayWin()
    {
        if (muted) return;
        float[] notes = { 523f, 659f, 784f, 1047f, 1319f };
        for (int i = 0; i < notes.Length; i++)
            StartCoroutine(DelayedTone(notes[i], 0.25f, i * 0.13f, true, 0.12f));
    }

    public void PlayFootstep()
    {
        if (muted) return;
        PlayTone(80f + Random.value * 40f, 0.04f, 0.02f);
    }

    public void PlayMilestone()
    {
        if (muted) return;
        PlayTone(880f, 0.08f, 0.1f);
        StartCoroutine(DelayedTone(1100f, 0.12f, 0.07f, true, 0.08f));
        StartCoroutine(DelayedTone(1320f, 0.15f, 0.14f, true, 0.06f));
    }

    public void PlayNPC()
    {
        if (muted) return;
        PlayTone(400f, 0.06f, 0.06f);
        StartCoroutine(DelayedTone(500f, 0.06f, 0.06f, true, 0.06f));
    }

    // ---- BACKGROUND MUSIC ----

    public void StartBGMusic()
    {
        if (bgMusicRunning) return;
        bgMusicRunning = true;
        StartCoroutine(BGMusicLoop());
    }

    public void StopBGMusic()
    {
        bgMusicRunning = false;
    }

    private IEnumerator BGMusicLoop()
    {
        float[][] chords = {
            new float[] { 261f, 329f, 392f },
            new float[] { 293f, 349f, 440f },
            new float[] { 329f, 415f, 493f },
            new float[] { 349f, 440f, 523f }
        };

        while (bgMusicRunning)
        {
            if (!muted && GameManager.Instance != null && GameManager.Instance.currentState == GameManager.GameState.Playing)
            {
                float[] chord = chords[Random.Range(0, chords.Length)];
                foreach (float freq in chord)
                    PlayTone(freq, 1.5f, 0.015f);
            }
            yield return new WaitForSeconds(3f);
        }
    }

    // ---- LOW-LEVEL AUDIO ----

    private void PlayTone(float frequency, float duration, float volume = 0.12f, bool sine = true)
    {
        int sampleRate = 44100;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = 1f - (float)i / sampleCount;
            if (sine)
                samples[i] = volume * envelope * Mathf.Sin(2f * Mathf.PI * frequency * t);
            else
                samples[i] = volume * envelope * Mathf.Sign(Mathf.Sin(2f * Mathf.PI * frequency * t)) * 0.5f;
        }

        AudioClip clip = AudioClip.Create("tone_" + frequency, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        sfxSource.PlayOneShot(clip);
        StartCoroutine(DestroyClip(clip, duration + 0.5f));
    }

    private IEnumerator DelayedTone(float freq, float dur, float delay, bool sine = true, float vol = -1f)
    {
        yield return new WaitForSeconds(delay);
        PlayTone(freq, dur, vol < 0 ? 0.12f : vol, sine);
    }

    private IEnumerator DestroyClip(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (clip != null) Destroy(clip);
    }
}
