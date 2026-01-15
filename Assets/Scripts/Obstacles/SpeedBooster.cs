using UnityEngine;

public class SpeedBooster : ObstacleBase
{
    [SerializeField] private float _boostMultiplier = 3f;

    protected override void OnSquareHit(SquareController square)
    {
        if (square.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity *= _boostMultiplier;
        }
    }
}