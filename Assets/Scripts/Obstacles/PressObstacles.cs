using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PressObstacle : MonoBehaviour
{
    [BoxGroup("General")]
    [SerializeField] private bool _playOnAwake = true;

    [BoxGroup("Scale Config")]
    [HorizontalGroup("Scale Config/Start")]
    [SerializeField] private Vector3 _startScale = Vector3.one;

    [HorizontalGroup("Scale Config/Start")]
    [Button("Set Current", ButtonSizes.Small), GUIColor(0.6f, 1f, 0.6f)]
    private void SetStartScale() => _startScale = transform.localScale;

    [BoxGroup("Scale Config")]
    [HorizontalGroup("Scale Config/End")]
    [SerializeField] private Vector3 _endScale = new Vector3(1, 10, 1);

    [HorizontalGroup("Scale Config/End")]
    [Button("Set Current", ButtonSizes.Small), GUIColor(1f, 0.6f, 0.6f)]
    private void SetEndScale() => _endScale = transform.localScale;

    [BoxGroup("Animation")]
    [SerializeField, SuffixLabel("s")] private float _duration = 2f;
    [BoxGroup("Animation")]
    [SerializeField, SuffixLabel("s")] private float _startDelay = 0f;
    [BoxGroup("Animation")]
    [SerializeField] private Ease _easeType = Ease.InOutQuad;
    [BoxGroup("Animation")]
    [SerializeField] private int _loops = 0;
    [BoxGroup("Animation")]
    [SerializeField] private LoopType _loopType = LoopType.Yoyo;

    [BoxGroup("Crush Logic")]
    [SerializeField] private bool _crushOnOverlap = true;
    [BoxGroup("Crush Logic")]
    [ShowIf("_crushOnOverlap")]
    [SerializeField] private LayerMask _targetLayer;
    [BoxGroup("Crush Logic")]
    [ShowIf("_crushOnOverlap")]
    [SerializeField] private int _crushDamage = 9999;

    [FoldoutGroup("Events")]
    [SerializeField] private UnityEvent _onSequenceComplete;
    [FoldoutGroup("Events")]
    [SerializeField] private UnityEvent _onLoopCycleComplete;

    private Rigidbody2D _rb;
    private Collider2D _col;
    private Tween _tween;
    private ContactFilter2D _contactFilter;
    private readonly List<Collider2D> _overlapResults = new List<Collider2D>();

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();

        ConfigurePhysics();

        _contactFilter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = true,
            layerMask = _targetLayer
        };
    }

    private void Start()
    {
        if (_playOnAwake)
        {
            StartCoroutine(WaitToStartRoutine());
        }
    }

    private void ConfigurePhysics()
    {
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.useFullKinematicContacts = true;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    private IEnumerator WaitToStartRoutine()
    {
        transform.localScale = _startScale;

        while (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
        {
            yield return null;
        }

        StartPressSequence();
    }

    private void FixedUpdate()
    {
        if (_crushOnOverlap)
        {
            CheckForCrush();
        }
    }

    private void CheckForCrush()
    {
        int count = _col.Overlap(_contactFilter, _overlapResults);

        for (int i = 0; i < count; i++)
        {
            if (_overlapResults[i].attachedRigidbody == null) continue;

            if (_overlapResults[i].transform.root.TryGetComponent<IDamageable>(out var victim))
            {
                victim.TakeDamage(_crushDamage);
            }
        }
    }

    [BoxGroup("Runtime Controls")]
    [Button("PLAY SEQUENCE", ButtonSizes.Large), GUIColor(0, 1, 0)]
    [ShowIf("@UnityEngine.Application.isPlaying")]
    public void StartPressSequence()
    {
        StopSequence();

        transform.localScale = _startScale;

        _tween = transform.DOScale(_endScale, _duration)
            .SetDelay(_startDelay)
            .SetEase(_easeType)
            .SetLoops(_loops, _loopType)
            .SetUpdate(UpdateType.Fixed)
            .OnStepComplete(() => _onLoopCycleComplete?.Invoke())
            .OnComplete(() => _onSequenceComplete?.Invoke());
    }

    [BoxGroup("Runtime Controls")]
    [Button("STOP", ButtonSizes.Medium), GUIColor(1, 0.5f, 0)]
    [ShowIf("@UnityEngine.Application.isPlaying")]
    public void StopSequence()
    {
        if (_tween != null) _tween.Kill();
    }

    [BoxGroup("Runtime Controls")]
    [Button("RESET", ButtonSizes.Medium), GUIColor(0.8f, 0.8f, 0.8f)]
    public void ResetToStart()
    {
        StopSequence();
        transform.localScale = _startScale;
    }

    private void OnDestroy()
    {
        if (_tween != null) _tween.Kill();
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.DrawWireCube(Vector3.zero, _startScale);

        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawWireCube(Vector3.zero, _endScale);

        Gizmos.color = Color.yellow;
        Vector3 direction = (_endScale - _startScale).normalized;
        if (direction != Vector3.zero)
        {
            Gizmos.DrawLine(Vector3.zero, direction * 2f);
        }
    }
}