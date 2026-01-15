using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ExplosiveProjectile : MonoBehaviour
{
    [SerializeField] private bool _alignToVelocity = true;
    [SerializeField] private float _explosionRadius = 3f;
    [SerializeField] private GameObject _explosionVfxPrefab;

    private int _damage;
    private Rigidbody2D _rb;
    private Collider2D _myCollider;
    private Collider2D _ownerCollider;
    private bool _hasExploded;

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
            float angle = Mathf.Atan2(_rb.linearVelocity.y, _rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    public void Setup(Vector2 direction, float speed, int damage, Collider2D ownerCollider)
    {
        _damage = damage;
        _ownerCollider = ownerCollider;
        _rb.linearVelocity = direction.normalized * speed;

        if (_alignToVelocity)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
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
        if (_hasExploded || other.isTrigger) return;
        if (other == _ownerCollider) return;

        Explode();
    }

    private void Explode()
    {
        _hasExploded = true;

        if (_explosionVfxPrefab != null)
        {
            Instantiate(_explosionVfxPrefab, transform.position, Quaternion.identity);
        }
        else if (EffectManager.Instance != null)
        {
            EffectManager.Instance.PlayEffect(EffectType.DeathExplosion, transform.position, Color.red);
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, _explosionRadius);
        foreach (var hit in hits)
        {
            if (hit == _ownerCollider) continue;
            if (hit.transform.root == _ownerCollider.transform.root) continue;

            if (hit.TryGetComponent<IDamageable>(out var target))
            {
                float distance = Vector2.Distance(transform.position, hit.transform.position);
                float falloff = Mathf.Clamp01(1f - (distance / _explosionRadius));
                int finalDmg = Mathf.RoundToInt(_damage * (0.5f + falloff * 0.5f));

                target.TakeDamage(finalDmg);
            }
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
    }
}