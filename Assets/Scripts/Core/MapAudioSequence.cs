using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MapAudioSequencer : MonoBehaviour
{
    private static MapAudioSequencer _instance;
    public static MapAudioSequencer Instance => _instance;

    [Title("Runtime Status")]
    [SerializeField, ReadOnly] private string _currentSong;
    [ShowInInspector, ReadOnly, ProgressBar(0, 1)] private float _beatProgress;

    [Title("Playback Control")]
    [SerializeField, Range(0.1f, 3.0f), OnValueChanged("RecalculateTiming")]
    private float _bpmMultiplier = 1.0f;

    [Title("Sequence Config")]
    [SerializeField, ReadOnly] private List<AudioClip> _runtimeSequence;
    [SerializeField, ReadOnly] private float _baseBpm = 120f;
    [SerializeField, ReadOnly] private int _beatDivision = 2;

    [Title("Audio Settings")]
    [SerializeField, Range(0f, 1f)] private float _globalVolume = 0.8f;
    [SerializeField] private AudioSource _audioSourcePrefab;
    [SerializeField] private int _poolSize = 20;
    [SerializeField, Range(0f, 1f)] private float _spatialBlend = 0.4f;

    [Title("Humanization")]
    [SerializeField, Range(0f, 0.1f)] private float _pitchRandomness = 0.01f;

    private int _sequenceIndex;
    private int _sequenceLength;
    private AudioSource[] _pool;
    private int _poolCursor;

    private double _lastNoteTime;
    private double _secondsPerStep;

    [Title("Editor Tools")]
    [SerializeField, FolderPath] private string _sequenceFolder;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        InitializePool();
        RecalculateTiming();
        _sequenceLength = _runtimeSequence != null ? _runtimeSequence.Count : 0;
    }

    private void Update()
    {
        if (_secondsPerStep > 0)
        {
            double timeSinceLast = Time.timeAsDouble - _lastNoteTime;
            _beatProgress = (float)(timeSinceLast / _secondsPerStep);
        }
    }

    private void InitializePool()
    {
        _pool = new AudioSource[_poolSize];
        GameObject poolContainer = new GameObject("AudioPool");
        poolContainer.transform.SetParent(transform);

        for (int i = 0; i < _poolSize; i++)
        {
            AudioSource source = Instantiate(_audioSourcePrefab, poolContainer.transform);
            source.playOnAwake = false;
            source.spatialBlend = _spatialBlend;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 2f;
            source.maxDistance = 30f;
            _pool[i] = source;
        }
    }

    private void RecalculateTiming()
    {
        if (_baseBpm <= 0) _baseBpm = 120;
        if (_beatDivision <= 0) _beatDivision = 1;

        float effectiveBpm = _baseBpm * _bpmMultiplier;

        // Safety check to prevent divide by zero
        if (effectiveBpm <= 0.1f) effectiveBpm = 0.1f;

        _secondsPerStep = (60d / effectiveBpm) / _beatDivision;
    }

    public void SetBpmMultiplier(float value)
    {
        _bpmMultiplier = Mathf.Clamp(value, 0.1f, 5.0f);
        RecalculateTiming();
    }

    public bool TryPlayNextHit(Vector3 position)
    {
        if (_sequenceLength == 0) return false;

        double currentTime = Time.timeAsDouble;

        if (currentTime - _lastNoteTime < _secondsPerStep)
        {
            return false;
        }

        _lastNoteTime = currentTime;

        AudioClip clip = _runtimeSequence[_sequenceIndex];
        PlayFromPool(clip, position);

        unchecked
        {
            _sequenceIndex++;
            if (_sequenceIndex >= _sequenceLength)
            {
                _sequenceIndex = 0;
            }
        }

        return true;
    }

    private void PlayFromPool(AudioClip clip, Vector3 position)
    {
        AudioSource source = _pool[_poolCursor];

        source.transform.position = position;
        source.volume = _globalVolume;
        source.clip = clip;

        // Pitch Logic: Base Multiplier + Small Randomness
        // Higher BPM = Higher Pitch to keep notes short and snappy
        float randomVar = Random.Range(-_pitchRandomness, _pitchRandomness);
        source.pitch = _bpmMultiplier + randomVar;

        source.Play();

        unchecked
        {
            _poolCursor++;
            if (_poolCursor >= _poolSize)
            {
                _poolCursor = 0;
            }
        }
    }

#if UNITY_EDITOR
    [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1)]
    private void GenerateSequenceFromFolder()
    {
        if (string.IsNullOrEmpty(_sequenceFolder)) return;

        string[] jsonFiles = Directory.GetFiles(_sequenceFolder, "*.json");
        if (jsonFiles.Length == 0)
        {
            Debug.LogError("No JSON found.");
            return;
        }

        string jsonContent = File.ReadAllText(jsonFiles[0]);
        SequenceData data = JsonUtility.FromJson<SequenceData>(jsonContent);

        if (data == null || data.clipSequence == null) return;

        _currentSong = data.songName;
        _baseBpm = data.bpm;
        _beatDivision = data.division > 0 ? data.division : 1;

        _runtimeSequence = new List<AudioClip>();
        string folderAssetPath = _sequenceFolder;
        if (folderAssetPath.StartsWith(Application.dataPath))
            folderAssetPath = "Assets" + folderAssetPath.Substring(Application.dataPath.Length);

        Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();

        foreach (string clipName in data.clipSequence)
        {
            if (!clipCache.TryGetValue(clipName, out AudioClip clip))
            {
                string clipPath = $"{folderAssetPath}/{clipName}";
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip != null) clipCache[clipName] = clip;
            }
            if (clip != null) _runtimeSequence.Add(clip);
        }

        // Reset multiplier to 1 when loading new song
        _bpmMultiplier = 1.0f;
        RecalculateTiming();

        EditorUtility.SetDirty(this);
        Debug.Log($"<color=green>Loaded: {data.songName} | BPM: {_baseBpm} | Notes: {_runtimeSequence.Count}</color>");
    }

    [System.Serializable]
    private class SequenceData
    {
        public string songName;
        public float bpm;
        public int division;
        public string[] clipSequence;
    }
#endif
}