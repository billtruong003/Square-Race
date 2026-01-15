using UnityEngine;

public abstract class ObstacleBase : MonoBehaviour
{
    protected abstract void OnSquareHit(SquareController square);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<SquareController>(out var square))
        {
            OnSquareHit(square);
        }
    }
}