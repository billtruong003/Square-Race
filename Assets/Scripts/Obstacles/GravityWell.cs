using UnityEngine;

public class GravityWell : MonoBehaviour
{
    [SerializeField] private float _pullForce = 20f;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.attachedRigidbody)
        {
            Vector2 dir = (transform.position - other.transform.position).normalized;
            other.attachedRigidbody.AddForce(dir * _pullForce);
        }
    }
}