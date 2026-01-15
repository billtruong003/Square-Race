using UnityEngine;

public class MagnumPistol : WeaponBase
{
    [Header("Magnum Config")]
    [SerializeField] private Projectile _projectilePrefab;
    [SerializeField] private Transform _firePoint;

    [Header("Stats")]
    [SerializeField] private float _fireRate = 1.5f;
    [SerializeField] private float _range = 18f;
    [SerializeField] private float _projectileSpeed = 50f;
    [SerializeField] private float _turnSpeed = 3000f;
    [SerializeField] private float _fireAngleTolerance = 5f;

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

        if (Quaternion.Angle(transform.rotation, targetRotation) <= _fireAngleTolerance && Time.time >= _nextFireTime)
        {
            Fire();
            _nextFireTime = Time.time + _fireRate;
        }
    }

    private void Fire()
    {
        if (_projectilePrefab == null) return;

        Projectile proj = Instantiate(_projectilePrefab, _firePoint.position, transform.rotation);
        proj.transform.localScale = Vector3.one * 1.2f;

        proj.Setup(transform.right, _projectileSpeed, Damage, OwnerCollider);

        ConsumeAmmo();
        if (AudioManager.Instance != null && _shootSound != null)
            AudioManager.Instance.PlaySFX(_shootSound, _firePoint.position);
    }
}