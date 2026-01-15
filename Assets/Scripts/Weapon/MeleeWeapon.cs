using UnityEngine;
using System.Collections.Generic;

public class MeleeWeapon : WeaponBase
{
    [Header("Melee Config")]
    [SerializeField] private float _rotationSpeed = 360f;
    [SerializeField] private float _damageInterval = 0.2f;
    [SerializeField] private float _durabilityLossRate = 1f;
    [SerializeField] private bool _spin = true;

    [Header("Visual Feedback")]
    [SerializeField] private float _hitShakeStrength = 0.1f;
    [SerializeField] private AudioClip _grindSound;

    private float _nextDamageTime;
    private float _nextDurabilityDropTime;
    private readonly HashSet<IDamageable> _activeTargets = new HashSet<IDamageable>();
    private AudioSource _audioSource;

    public override void Initialize(Transform owner, Collider2D ownerCollider)
    {
        base.Initialize(owner, ownerCollider);

        if (_grindSound != null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.clip = _grindSound;
            _audioSource.loop = true;
            _audioSource.volume = 0f;
            _audioSource.playOnAwake = false;
            _audioSource.Play();
        }
    }

    private void Update()
    {
        if (_spin)
        {
            transform.Rotate(Vector3.forward, _rotationSpeed * Time.deltaTime);
        }

        ProcessContinuousDamage();
        UpdateAudio();
    }

    private void ProcessContinuousDamage()
    {
        _activeTargets.RemoveWhere(t => t == null || (t as Object) == null);

        if (_activeTargets.Count > 0)
        {
            if (Time.time >= _nextDamageTime)
            {
                foreach (var target in _activeTargets)
                {
                    target.TakeDamage(Damage);
                    transform.localPosition += (Vector3)Random.insideUnitCircle * _hitShakeStrength;
                }
                _nextDamageTime = Time.time + _damageInterval;
            }

            if (Time.time >= _nextDurabilityDropTime)
            {
                ConsumeAmmo();
                _nextDurabilityDropTime = Time.time + (1f / _durabilityLossRate);
            }
        }
        else
        {
            if (transform.localPosition.sqrMagnitude > 0.01f)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, Vector3.zero, Time.deltaTime * 10f);
            }
        }
    }

    private void UpdateAudio()
    {
        if (_audioSource == null) return;
        float targetVolume = _activeTargets.Count > 0 ? 0.8f : 0f;
        _audioSource.volume = Mathf.Lerp(_audioSource.volume, targetVolume, Time.deltaTime * 10f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == OwnerCollider) return;
        if (other.isTrigger) return;

        if (other.transform.root == Owner) return;

        if (other.TryGetComponent<IDamageable>(out var target))
        {
            _activeTargets.Add(target);
            target.TakeDamage(Damage);

            if (EffectManager.Instance != null)
            {
                EffectManager.Instance.PlayEffect(EffectType.WallImpact, other.ClosestPoint(transform.position), Color.white);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent<IDamageable>(out var target))
        {
            _activeTargets.Remove(target);
        }
    }

    private void OnDisable()
    {
        _activeTargets.Clear();
    }
}