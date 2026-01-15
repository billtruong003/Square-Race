using UnityEngine;

public class Shotgun : WeaponBase
{
    [Header("Shotgun Config")]
    [SerializeField] private Projectile _projectilePrefab;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private int _pelletCount = 5;
    [SerializeField] private float _spreadAngle = 30f;

    [Header("Stats")]
    [SerializeField] private float _fireRate = 1.2f;
    [SerializeField] private float _range = 8f;
    [SerializeField] private float _projectileSpeed = 25f;
    [SerializeField] private float _turnSpeed = 1500f;
    [SerializeField] private float _fireAngleTolerance = 20f;

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

        float startAngle = -_spreadAngle / 2f;
        float angleStep = _spreadAngle / Mathf.Max(1, _pelletCount - 1);
        float currentBaseAngle = transform.rotation.eulerAngles.z;

        for (int i = 0; i < _pelletCount; i++)
        {
            float currentAngleOffset = startAngle + (angleStep * i);
            Quaternion pelletRot = Quaternion.Euler(0, 0, currentBaseAngle + currentAngleOffset);

            Projectile proj = Instantiate(_projectilePrefab, _firePoint.position, pelletRot);
            proj.transform.localScale = Vector3.one * 0.7f;

            Vector2 dir = pelletRot * Vector3.right;
            proj.Setup(dir, _projectileSpeed, Damage, OwnerCollider);
        }

        ConsumeAmmo();
        if (AudioManager.Instance != null && _shootSound != null)
            AudioManager.Instance.PlaySFX(_shootSound, _firePoint.position);
    }
}