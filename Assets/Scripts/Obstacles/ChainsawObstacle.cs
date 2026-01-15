using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections.Generic;

[RequireComponent(typeof(CircleCollider2D), typeof(Rigidbody2D))]
public class ChainsawObstacle : MonoBehaviour
{
    [BoxGroup("Movement")]
    [SerializeField] private bool _usePathMovement = false;
    [BoxGroup("Movement")]
    [ShowIf("_usePathMovement")]
    [SerializeField] private List<Transform> _waypoints;
    [BoxGroup("Movement")]
    [ShowIf("_usePathMovement")]
    [SerializeField, SuffixLabel("s")] private float _moveDuration = 5f;
    [BoxGroup("Movement")]
    [ShowIf("_usePathMovement")]
    [SerializeField] private Ease _moveEase = Ease.Linear;

    [BoxGroup("Blocking Logic")]
    [ShowIf("_usePathMovement")]
    [Tooltip("Layer làm cưa dừng di chuyển (chọn DeadBody và Wall)")]
    [SerializeField] private LayerMask _blockingLayers;

    [BoxGroup("Rotation")]
    [SerializeField] private Transform _visualModel;
    [BoxGroup("Rotation")]
    [SerializeField] private float _rotateSpeed = 720f;

    [BoxGroup("Damage")]
    [SerializeField] private float _knockbackForce = 20f;

    [BoxGroup("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [BoxGroup("Audio")]
    [SerializeField] private AudioClip _grindSound; // Tiếng cưa xác "Rè rè"

    private Tween _moveTween;
    private Tween _rotateTween;
    private bool _isBlocked = false;

    private void Start()
    {
        SetupRotation();
        if (_usePathMovement && _waypoints.Count > 1)
        {
            SetupPathMovement();
        }

        // Cưa là Kinematic để di chuyển bằng Tween nhưng vẫn check được Trigger
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void SetupRotation()
    {
        if (_visualModel != null)
        {
            // Luôn luôn xoay, không bao giờ dừng
            _rotateTween = _visualModel.DORotate(new Vector3(0, 0, 360), 1f, RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Restart)
                .SetEase(Ease.Linear)
                .SetRelative()
                .SetUpdate(UpdateType.Fixed);

            _rotateTween.timeScale = _rotateSpeed / 360f;
        }
    }

    private void SetupPathMovement()
    {
        Vector3[] path = new Vector3[_waypoints.Count];
        for (int i = 0; i < _waypoints.Count; i++)
        {
            path[i] = _waypoints[i].position;
        }

        transform.position = path[0];

        _moveTween = transform.DOPath(path, _moveDuration, PathType.CatmullRom)
            .SetOptions(true)
            .SetEase(_moveEase)
            .SetLoops(-1)
            .SetUpdate(UpdateType.Fixed);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Xử lý giết người (Nếu còn sống)
        if (other.TryGetComponent<SquareController>(out var racer))
        {
            // Giết -> Biến thành Static Body (Layer DeadBody)
            racer.DieAsSolidObstacle();

            // Play tiếng xay thịt
            if (_audioSource != null && _grindSound != null) _audioSource.PlayOneShot(_grindSound);

            // Visual feedback: Rung lắc mạnh vì đang xay
            // transform.DOShakePosition(0.2f, 0.3f);
        }

        // 2. Xử lý bị chặn (Bởi DeadBody vừa tạo ra hoặc Tường)
        // Check xem layer của object vừa chạm có nằm trong BlockingLayers không
        if (((1 << other.gameObject.layer) & _blockingLayers) != 0)
        {
            BlockMovement();
        }
    }

    private void BlockMovement()
    {
        if (_isBlocked || _moveTween == null) return;

        _isBlocked = true;
        _moveTween.Pause(); // Dừng di chuyển

        // Vẫn xoay (RotateTween không bị kill)
        Debug.Log("Chainsaw bị chặn bởi xác chết hoặc tường!");
    }

    // Optional: Nếu muốn cưa tiếp tục chạy khi xác biến mất (nếu game có cơ chế dọn xác)
    private void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & _blockingLayers) != 0)
        {
            // Kiểm tra xem còn vật cản nào khác không trước khi chạy lại
            if (!IsTouchingLayer(_blockingLayers))
            {
                _isBlocked = false;
                if (_moveTween != null) _moveTween.Play();
            }
        }
    }

    private bool IsTouchingLayer(LayerMask layer)
    {
        return GetComponent<Collider2D>().IsTouchingLayers(layer);
    }

    private void OnDestroy()
    {
        if (_moveTween != null) _moveTween.Kill();
        if (_rotateTween != null) _rotateTween.Kill();
        transform.DOKill();
    }

    private void OnDrawGizmos()
    {
        if (!_usePathMovement || _waypoints == null || _waypoints.Count < 2) return;
        Gizmos.color = Color.red;
        for (int i = 0; i < _waypoints.Count; i++)
        {
            if (_waypoints[i] != null && _waypoints[(i + 1) % _waypoints.Count] != null)
                Gizmos.DrawLine(_waypoints[i].position, _waypoints[(i + 1) % _waypoints.Count].position);
        }
    }
}