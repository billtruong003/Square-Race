using UnityEngine;

public class RacerConfigurator : MonoBehaviour
{
    [SerializeField] private float _globalSpeed = 15f;
    [SerializeField] private float _raycastDistance = 0.8f;
    [SerializeField] private LayerMask _wallLayer;
    [SerializeField] private bool _randomizeStartDirection = true;

    [ContextMenu("Apply To All Children")]
    public void ApplySettings()
    {
        SquareController[] racers = GetComponentsInChildren<SquareController>();

        if (racers.Length == 0) return;

        foreach (var racer in racers)
        {
            racer.ApplySettings(_globalSpeed, _raycastDistance, _wallLayer);
            racer.SetInitialDirection(_randomizeStartDirection ? Random.insideUnitCircle.normalized : Vector2.up);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(racer);
#endif
        }
    }
}