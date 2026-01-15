using UnityEngine;

public static class GameConstants
{
    public static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    public static readonly int EmissionIntensityId = Shader.PropertyToID("_EmissionIntensity");

    public const string MenuScene = "Menu";
    public const string MapSelectScene = "MapSelect";
    public const string StagingScene = "StagingScene";

    public static class Tags
    {
        public const string Player = "Player";
    }
}