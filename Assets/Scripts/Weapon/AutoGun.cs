using UnityEngine;

public class AutoGun : WeaponBase
{
    [Header("Gun Config")]
    [SerializeField] private Projectile _projectilePrefab;
    [SerializeField] private Transform _firePoint;

    [Header("Stats")]
    [SerializeField] private float _fireRate = 0.2f;
    [SerializeField] private float _range = 15f;
    [SerializeField] private float _projectileSpeed = 35f;
    [SerializeField] private float _turnSpeed = 2000f;
    [SerializeField] private float _fireAngleTolerance = 15f;

    [Header("Audio")]
    [SerializeField] private AudioClip _shootSound;

    private float _nextFireTime;
    private Transform _currentTarget;

    private void OnValidate()
    {
        if (_firePoint == null) _firePoint = transform;
    }

    private void Update()
    {
        if (Owner == null) return;

        UpdateTarget();
        RotateAndShoot();
    }

    private void UpdateTarget()
    {
        if (IsTargetValid(_currentTarget)) return;
        _currentTarget = FindNearestEnemy(_range);
    }

    private bool IsTargetValid(Transform target)
    {
        if (target == null || !target.gameObject.activeSelf) return false;
        float distSq = (target.position - transform.position).sqrMagnitude;
        return distSq <= _range * _range;
    }

    private void RotateAndShoot()
    {
        if (_currentTarget == null) return;

        Vector3 dir = _currentTarget.position - transform.position;
        dir.z = 0;

        if (dir.sqrMagnitude < 0.001f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _turnSpeed * Time.deltaTime);

        Vector3 scale = transform.localScale;
        scale.y = (Mathf.Abs(angle) > 90f) ? -Mathf.Abs(scale.y) : Mathf.Abs(scale.y);
        transform.localScale = scale;

        float angleDiff = Quaternion.Angle(transform.rotation, targetRotation);
        if (angleDiff <= _fireAngleTolerance && Time.time >= _nextFireTime)
        {
            Fire();
            _nextFireTime = Time.time + _fireRate;
        }
    }

    private void Fire()
    {
        if (_projectilePrefab == null) return;

        Projectile proj = Instantiate(_projectilePrefab, _firePoint.position, transform.rotation);
        proj.transform.localScale = Vector3.one;
        proj.Setup(transform.right, _projectileSpeed, Damage, OwnerCollider);

        ConsumeAmmo();

        if (AudioManager.Instance != null && _shootSound != null)
        {
            AudioManager.Instance.PlaySFX(_shootSound, _firePoint.position);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 1, 0.2f);
        Gizmos.DrawWireSphere(transform.position, _range);

        if (_currentTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, _currentTarget.position);
        }
    }
}