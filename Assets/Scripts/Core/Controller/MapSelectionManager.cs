using UnityEngine;

public class MapSelectionManager : Singleton<MapSelectionManager>
{
    public string SelectedMapSceneName { get; private set; } = "GameMap1";

    public void SelectMap(string mapName)
    {
        SelectedMapSceneName = mapName;
    }
}