using UnityEngine;

public class RocketLauncher : WeaponBase
{
    [Header("Rocket Config")]
    [SerializeField] private ExplosiveProjectile _rocketPrefab;
    [SerializeField] private Transform _firePoint;

    [Header("Stats")]
    [SerializeField] private float _fireRate = 2.0f;
    [SerializeField] private float _range = 20f;
    [SerializeField] private float _rocketSpeed = 15f;
    [SerializeField] private float _turnSpeed = 1000f;
    [SerializeField] private float _fireAngleTolerance = 10f;

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
        if (_rocketPrefab == null) return;

        ExplosiveProjectile rocket = Instantiate(_rocketPrefab, _firePoint.position, transform.rotation);
        rocket.transform.localScale = Vector3.one;

        rocket.Setup(transform.right, _rocketSpeed, Damage, OwnerCollider);

        ConsumeAmmo();
        if (AudioManager.Instance != null && _shootSound != null)
            AudioManager.Instance.PlaySFX(_shootSound, _firePoint.position);
    }
}