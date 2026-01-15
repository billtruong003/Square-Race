using UnityEngine;

public class GlitchZone : ObstacleBase
{
    protected override void OnSquareHit(SquareController square)
    {
        if (square.TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.y, rb.linearVelocity.x) * -1;
        }
    }
}