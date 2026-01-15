using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MusicalImpactData", menuName = "Audio/Musical Impact Data")]
public class ImpactAudioData : ScriptableObject
{
    [Header("Scale Config (Do Re Mi Fa Sol La Si)")]
    [SerializeField] private List<AudioClip> _notes;

    [Header("Settings")]
    [Range(0f, 2f)] public float Volume = 0.8f;
    [Range(0f, 0.5f)] public float PitchVariation = 0.1f;

    public AudioClip GetNote(int index)
    {
        if (_notes == null || _notes.Count == 0) return null;
        return _notes[Mathf.Abs(index) % _notes.Count];
    }

    public int NoteCount => _notes == null ? 0 : _notes.Count;
}