using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;

public enum WeaponReleaseMode
{
    TimeBased,
    AmmoBased
}

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Release Configuration")]
    [SerializeField] protected WeaponReleaseMode ReleaseMode = WeaponReleaseMode.AmmoBased;
    [SerializeField] protected int MaxAmmo = 10;
    [SerializeField] protected float LifeTime = 5f;
    [SerializeField] protected GameObject PickupPrefabToSpawnOnDrop;

    [Header("Base Settings")]
    [SerializeField] protected int Damage = 20;

    protected Transform Owner;
    protected Collider2D OwnerCollider;
    protected int CurrentAmmo;

    private bool _isInitialized;
    private Tween _scaleTween;
    private Collider2D _weaponCollider;

    private static List<SquareController> _cachedRacers;
    private static float _lastCacheTime;

    public virtual void Initialize(Transform owner, Collider2D ownerCollider)
    {
        Owner = owner;
        OwnerCollider = ownerCollider;
        CurrentAmmo = MaxAmmo;
        _isInitialized = true;
        _weaponCollider = GetComponent<Collider2D>();

        if (_weaponCollider != null && OwnerCollider != null)
        {
            Physics2D.IgnoreCollision(_weaponCollider, OwnerCollider, true);
        }

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        var sr = GetComponentInChildren<SpriteRenderer>();
        var ownerSr = owner.GetComponentInChildren<SpriteRenderer>();
        if (sr != null && ownerSr != null)
        {
            sr.sortingLayerName = ownerSr.sortingLayerName;
            sr.sortingOrder = ownerSr.sortingOrder + 1;
        }

        transform.localScale = Vector3.zero;
        _scaleTween = transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);

        if (ReleaseMode == WeaponReleaseMode.TimeBased)
        {
            Invoke(nameof(ForceDrop), LifeTime);
        }
    }

    protected void ConsumeAmmo()
    {
        if (ReleaseMode == WeaponReleaseMode.TimeBased) return;

        CurrentAmmo--;
        if (CurrentAmmo <= 0)
        {
            ForceDrop();
        }
    }

    public void ForceDrop()
    {
        if (!_isInitialized) return;

        Cleanup();

        if (PickupPrefabToSpawnOnDrop != null)
        {
            var pickup = Instantiate(PickupPrefabToSpawnOnDrop, transform.position, Quaternion.identity);

            if (pickup.TryGetComponent<PickupItem>(out var item))
            {
                item.SimulateDropPhysics();
            }
        }

        Destroy(gameObject);
    }

    private void Cleanup()
    {
        if (_weaponCollider != null && OwnerCollider != null)
        {
            Physics2D.IgnoreCollision(_weaponCollider, OwnerCollider, false);
        }

        _isInitialized = false;
        CancelInvoke();
        if (_scaleTween != null) _scaleTween.Kill();
        transform.SetParent(null);
    }

    protected Transform FindNearestEnemy(float range)
    {
        if (_cachedRacers == null || Time.time - _lastCacheTime > 1f)
        {
            if (RaceManager.Instance != null)
                _cachedRacers = RaceManager.Instance.GetAllRacers();
            else
                _cachedRacers = FindObjectsByType<SquareController>(FindObjectsSortMode.None).ToList();
            _lastCacheTime = Time.time;
        }

        Transform bestTarget = null;
        float minSqrDist = float.MaxValue;
        Vector3 myPos = transform.position;
        float rangeSqr = range * range;

        foreach (var racer in _cachedRacers)
        {
            if (racer == null || !racer.gameObject.activeSelf) continue;

            Transform t = racer.transform;
            if (t == Owner) continue;
            if (t.root == Owner) continue;

            float d2 = (t.position - myPos).sqrMagnitude;
            if (d2 < minSqrDist && d2 <= rangeSqr)
            {
                minSqrDist = d2;
                bestTarget = t;
            }
        }

        return bestTarget;
    }
}