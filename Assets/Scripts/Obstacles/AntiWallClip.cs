using UnityEngine;

// GẮN VÀO PLAYER / RACER
public class AntiWallClip : MonoBehaviour
{
    [SerializeField] private LayerMask _wallLayer;
    [SerializeField] private float _checkRadius = 0.1f; // Bán kính rất nhỏ ở tâm xe

    private IDamageable _damageable;

    private void Awake()
    {
        _damageable = GetComponent<IDamageable>();
    }

    private void FixedUpdate()
    {
        // Kiểm tra xem TÂM của xe có đang nằm trong tường không
        // OverlapCircle nhanh hơn nhiều so với OverlapCollider của cả cái map
        if (Physics2D.OverlapCircle(transform.position, _checkRadius, _wallLayer))
        {
            // Nếu tâm xe nằm trong tường -> Chết luôn
            _damageable.TakeDamage(9999);
        }
    }

    // Vẽ debug để xem vùng check
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _checkRadius);
    }
}