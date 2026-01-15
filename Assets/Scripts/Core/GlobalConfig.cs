using UnityEngine;

public class GlobalConfig : Singleton<GlobalConfig>
{
    [SerializeField] private SquareData _defaultSquareData;

    public SquareData GetSquareData() => _defaultSquareData;
}