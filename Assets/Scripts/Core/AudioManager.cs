using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : Singleton<AudioManager>
{
    [SerializeField] private AudioSource _sfxSourcePrefab;
    [SerializeField] private int _poolSize = 30;
    [SerializeField] private AudioSource _bgmSource;

    private readonly Queue<AudioSource> _sfxPool = new Queue<AudioSource>();
    private float _bgmTargetVolume = 1f;
    private Coroutine _fadeCoroutine;

    protected override void Awake()
    {
        base.Awake();
        InitializePool();
        EnsureBgmSource();
    }

    private void EnsureBgmSource()
    {
        if (_bgmSource != null) return;

        GameObject bgmObj = new GameObject("BGM_Source");
        bgmObj.transform.SetParent(transform);
        _bgmSource = bgmObj.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;
    }

    private void InitializePool()
    {
        GameObject poolRoot = new GameObject("SFX_Pool");
        poolRoot.transform.SetParent(transform);

        for (int i = 0; i < _poolSize; i++)
        {
            AudioSource source = Instantiate(_sfxSourcePrefab, poolRoot.transform);
            source.gameObject.SetActive(false);
            _sfxPool.Enqueue(source);
        }
    }

    public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetAudioSource();
        source.transform.position = position;
        source.gameObject.SetActive(true);
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.Play();

        StartCoroutine(DisableSourceDelayed(source, clip.length));
    }

    private AudioSource GetAudioSource()
    {
        if (_sfxPool.Count > 0) return _sfxPool.Dequeue();
        return Instantiate(_sfxSourcePrefab, transform);
    }

    private IEnumerator DisableSourceDelayed(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        source.Stop();
        source.clip = null;
        source.gameObject.SetActive(false);
        _sfxPool.Enqueue(source);
    }

    public void PlayMusic(AudioClip musicClip, float fadeDuration = 1.0f, float volume = 0.5f)
    {
        if (_bgmSource.clip == musicClip) return;

        _bgmTargetVolume = volume;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(CrossFadeMusic(musicClip, fadeDuration));
    }

    private IEnumerator CrossFadeMusic(AudioClip newClip, float duration)
    {
        float startVolume = _bgmSource.volume;
        float halfDuration = duration * 0.5f;

        for (float t = 0; t < halfDuration; t += Time.deltaTime)
        {
            _bgmSource.volume = Mathf.Lerp(startVolume, 0, t / halfDuration);
            yield return null;
        }

        _bgmSource.Stop();
        _bgmSource.clip = newClip;
        _bgmSource.Play();

        for (float t = 0; t < halfDuration; t += Time.deltaTime)
        {
            _bgmSource.volume = Mathf.Lerp(0, _bgmTargetVolume, t / halfDuration);
            yield return null;
        }

        _bgmSource.volume = _bgmTargetVolume;
    }
}