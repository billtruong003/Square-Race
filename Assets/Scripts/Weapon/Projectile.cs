using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private bool _alignToVelocity = true;

    private int _damage;
    private Rigidbody2D _rb;
    private Collider2D _myCollider;
    private bool _hasHit;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _myCollider = GetComponent<Collider2D>();

        _myCollider.isTrigger = true;
    }

    private void Update()
    {
        if (_alignToVelocity && _rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            float angle = Mathf.Atan2(_rb.linearVelocity.y, _rb.linearVelocity.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    public void Setup(Vector2 direction, float speed, int damage, Collider2D ownerCollider)
    {
        _damage = damage;
        _rb.linearVelocity = direction.normalized * speed;

        if (_alignToVelocity)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        if (ownerCollider != null)
        {
            Physics2D.IgnoreCollision(_myCollider, ownerCollider);
        }

        Destroy(gameObject, 3.0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasHit || other.isTrigger) return;

        if (other.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(_damage);
            _hasHit = true;

            HandleImpact();
        }
        else if (!other.CompareTag(GameConstants.Tags.Player))
        {
            _hasHit = true;
            HandleImpact();
        }
    }

    private void HandleImpact()
    {
        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.PlayEffect(EffectType.WallImpact, transform.position, Color.yellow);
        }

        Destroy(gameObject);
    }
}