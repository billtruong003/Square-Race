using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider2D))]
public class PickupItem : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private GameObject _weaponPrefab;
    [SerializeField] private AudioClip _pickupSound;
    [SerializeField] private LayerMask _wallLayer;

    [Header("Visuals")]
    [SerializeField] private Transform _visualModel;
    [SerializeField] private float _rotateSpeed = 180f;

    [Header("Drop Settings")]
    [SerializeField] private float _dropDistance = 2.0f;
    [SerializeField] private float _wallBuffer = 0.5f;

    private Collider2D _col;
    private bool _isCanPick = true;
    private bool _isPicked = false;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
    }

    private void Update()
    {
        if (_visualModel != null)
            _visualModel.Rotate(Vector3.forward, _rotateSpeed * Time.deltaTime);
    }

    public void SimulateDropPhysics()
    {
        _isCanPick = false;
        _isPicked = false;
        _col.enabled = false;

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        Vector3 targetPos = CalculateSafeDropPosition(randomDir);

        transform.DOJump(targetPos, 1f, 1, 0.5f)
            .OnStart(() =>
            {
                _col.enabled = true;
            })
            .OnComplete(() =>
            {
                _isCanPick = true;
            });
    }

    private Vector3 CalculateSafeDropPosition(Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, _dropDistance, _wallLayer);

        if (hit.collider != null)
        {
            return hit.point - (direction * _wallBuffer);
        }

        return transform.position + (Vector3)(direction * _dropDistance);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isCanPick || _isPicked) return;

        if (other.TryGetComponent<SquareController>(out var square))
        {
            if (_weaponPrefab != null)
            {
                _isPicked = true;
                _col.enabled = false;

                square.EquipWeapon(_weaponPrefab);

                if (AudioManager.Instance != null && _pickupSound != null)
                    AudioManager.Instance.PlaySFX(_pickupSound, transform.position);

                Destroy(gameObject);
            }
        }
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}