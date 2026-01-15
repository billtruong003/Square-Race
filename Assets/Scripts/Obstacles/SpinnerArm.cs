using UnityEngine;

public class SpinnerArm : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 100f;

    private void Update()
    {
        transform.Rotate(0, 0, _rotationSpeed * Time.deltaTime);
    }
}