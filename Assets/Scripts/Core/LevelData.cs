using UnityEngine;
using System.Collections.Generic;

public enum GameMode
{
    Racing,
    LastManStanding
}

public enum ColorAssignmentMode
{
    Gradient,
    FixedSequence
}

[CreateAssetMenu(fileName = "NewLevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("General Info")]
    public string LevelName;
    public string SceneName;
    public Sprite Thumbnail;
    [TextArea] public string Description;
    public int DifficultyRating = 1;
    public GameMode Mode = GameMode.Racing;

    [Header("Staging & Visuals Configuration")]
    public string StagingSceneName = "StagingScene";
    public int StagingRacerCount = 100;

    [Space(10)]
    public ColorAssignmentMode ColorMode = ColorAssignmentMode.Gradient;

    [Tooltip("Dùng khi Mode là Gradient")]
    public Gradient RacerColorGradient;

    [Tooltip("Dùng khi Mode là FixedSequence")]
    public List<Color> FixedColorSequence;

    [Space(10)]
    public List<Sprite> RacerShapes;

    [Header("Gameplay Configuration")]
    public Sprite DeathBodySprite;
}