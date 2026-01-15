using UnityEngine;

public class Teleporter : ObstacleBase
{
    [SerializeField] private Transform _exitPoint;

    protected override void OnSquareHit(SquareController square)
    {
        if (_exitPoint != null)
            square.transform.position = _exitPoint.position;
    }
}