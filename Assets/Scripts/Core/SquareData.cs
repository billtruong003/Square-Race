using UnityEngine;

[CreateAssetMenu(fileName = "SquareData", menuName = "Game/SquareData")]
public class SquareData : ScriptableObject
{
    public float MoveSpeed = 10f;
    public float BounceForce = 5f;
    public float MaxVelocity = 20f;
    public LayerMask WallLayer;
    public PhysicsMaterial2D BounceMaterial;
}