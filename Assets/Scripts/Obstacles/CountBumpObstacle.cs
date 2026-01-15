using UnityEngine;
using TMPro;
using DG.Tweening;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Collider2D))]
public class CountBumpObstacle : MonoBehaviour
{
    [BoxGroup("Game Logic")]
    [SerializeField] private int _totalHits = 5;

    [BoxGroup("Game Logic")]
    [SerializeField] private bool _requireColor = false;

    [BoxGroup("Game Logic")]
    [ShowIf("_requireColor")]
    [SerializeField] private Color _targetColor = Color.red;

    [BoxGroup("Visuals")]
    [SerializeField] private Transform _visualModel;
    [BoxGroup("Visuals")]
    [SerializeField] private SpriteRenderer _renderer;
    [BoxGroup("Visuals")]
    [SerializeField] private TextMeshPro _countText;
    [BoxGroup("Visuals")]
    [SerializeField] private Color _defaultColor = Color.gray;

    [BoxGroup("Animation")]
    [SerializeField] private float _shakeStrength = 0.5f;
    [BoxGroup("Animation")]
    [SerializeField] private float _punchDuration = 0.2f;

    [BoxGroup("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [BoxGroup("Audio")]
    [SerializeField] private AudioClip _hitSound;
    [BoxGroup("Audio")]
    [SerializeField] private AudioClip _breakSound;
    [BoxGroup("Audio")]
    [SerializeField] private AudioClip _wrongSound;

    private int _currentHitsLeft;
    private Tween _punchTween;
    private Collider2D _col;
    private float _lastInteractTime;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        InitializeState();
    }

    private void InitializeState()
    {
        _currentHitsLeft = _totalHits;
        UpdateVisuals();
        UpdateText();
    }

    private void UpdateVisuals()
    {
        if (_renderer != null)
        {
            _renderer.color = _requireColor ? _targetColor : _defaultColor;
        }
    }

    private void UpdateText()
    {
        if (_countText != null)
        {
            _countText.text = _currentHitsLeft.ToString();
        }
    }

    private void OnValidate()
    {
        UpdateVisuals();
        if (_countText != null) _countText.text = _totalHits.ToString();
    }

    // Hàm này được gọi bởi Raycast từ SquareController
    public void RegisterImpact(SquareController racer)
    {
        // Debounce: Chặn việc trừ nhiều lần trong 1 thời gian ngắn do Raycast quét liên tục
        if (Time.time < _lastInteractTime + 0.2f) return;
        _lastInteractTime = Time.time;

        if (_currentHitsLeft <= 0) return;

        if (CheckHitValidity(racer))
        {
            ProcessValidHit();
        }
        else
        {
            ProcessInvalidHit();
        }
    }

    // Fallback: Vẫn giữ va chạm vật lý nếu có vật thể khác (không phải Racer) tông vào
    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.TryGetComponent<SquareController>(out var racer))
        {
            RegisterImpact(racer);
        }
    }

    private bool CheckHitValidity(SquareController racer)
    {
        if (!_requireColor) return true;
        return IsColorSimilar(racer.GetColor(), _targetColor);
    }

    private void ProcessValidHit()
    {
        _currentHitsLeft--;
        UpdateText();

        if (_visualModel != null)
        {
            if (_punchTween != null && _punchTween.IsActive()) _punchTween.Complete();
            _punchTween = _visualModel.DOPunchScale(Vector3.one * 0.2f, _punchDuration, 10, 1);
        }

        if (_currentHitsLeft <= 0)
        {
            BreakObject();
        }
        else
        {
            if (_audioSource != null && _hitSound != null)
                _audioSource.PlayOneShot(_hitSound);
        }
    }

    private void ProcessInvalidHit()
    {
        if (_visualModel != null)
        {
            if (_punchTween != null && _punchTween.IsActive()) _punchTween.Complete();
            _punchTween = _visualModel.DOShakePosition(_punchDuration, _shakeStrength, 20, 90, false, true);
        }

        if (_audioSource != null && _wrongSound != null)
            _audioSource.PlayOneShot(_wrongSound);
    }

    private void BreakObject()
    {
        if (_audioSource != null && _breakSound != null)
        {
            AudioSource.PlayClipAtPoint(_breakSound, transform.position);
        }

        if (EffectManager.Instance != null)
        {
            Color effectColor = _requireColor ? _targetColor : _defaultColor;
            EffectManager.Instance.PlayEffect(EffectType.WallImpact, transform.position, effectColor);
        }

        _col.enabled = false;
        if (_visualModel != null) _visualModel.gameObject.SetActive(false);
        if (_countText != null) _countText.gameObject.SetActive(false);

        Destroy(gameObject, 1f);
    }

    private bool IsColorSimilar(Color a, Color b, float tolerance = 0.1f)
    {
        float diff = Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
        return diff < tolerance;
    }
}