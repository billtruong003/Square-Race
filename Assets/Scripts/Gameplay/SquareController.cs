using UnityEngine;
using System.Collections;
using ShootingVR.Visuals;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class SquareController : MonoBehaviour, IDamageable
{
    [Header("Visuals")]
    [SerializeField] private SquareVisual _visuals;
    [SerializeField] private Sprite _fallbackDeadSprite;
    [SerializeField] private GpuTrail _myTrail;
    [SerializeField] private float _defaultEmissionIntensity = 3.5f;

    [Header("Death Settings")]
    [SerializeField] private bool _isPermanentDeathBody = false;
    [SerializeField] private float _bodyDespawnDelay = 3.0f;

    [Header("Audio")]
    [SerializeField] private AudioClip _deathSfx;
    [Range(0f, 1f)][SerializeField] private float _sfxVolume = 0.8f;

    [Header("Movement Config")]
    [SerializeField] private float _moveSpeed = 15f;
    [SerializeField] private float _rayDistance = 1.2f;
    [SerializeField] private LayerMask _obstacleLayer;
    [SerializeField] private LayerMask _interactableLayer;

    [Header("Direction Debug")]
    [Range(0f, 360f)][SerializeField] private float _startAngle = 0f;

    private Rigidbody2D _rb;
    private Collider2D _myCollider;
    private bool _isRacing;
    private int _currentHealth = 100;
    private Vector2 _currentDirection = Vector2.up;
    private Color _myColor = Color.white;
    private Sprite _originalSprite;
    private Coroutine _flashRoutine;

    private readonly RaycastHit2D[] _rayHits = new RaycastHit2D[5];
    private static readonly Vector2[] _checkDirections = {
        Vector2.up, new Vector2(0.7071f, 0.7071f), Vector2.right, new Vector2(0.7071f, -0.7071f),
        Vector2.down, new Vector2(-0.7071f, -0.7071f), Vector2.left, new Vector2(-0.7071f, 0.7071f)
    };

    private void OnValidate() => UpdateDirectionFromAngle();

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _myCollider = GetComponent<Collider2D>();
        if (_visuals == null) _visuals = GetComponent<SquareVisual>();

        UpdateDirectionFromAngle();
        ConfigurePhysics();
    }

    private void UpdateDirectionFromAngle()
    {
        float rad = _startAngle * Mathf.Deg2Rad;
        _currentDirection = new Vector2(Mathf.Sin(-rad), Mathf.Cos(rad)).normalized;
    }

    private void ConfigurePhysics()
    {
        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.gravityScale = 0;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    public void Initialize()
    {
        _currentHealth = 100;
        _isRacing = false;
        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.linearVelocity = Vector2.zero;
        _myCollider.enabled = true;

        if (_myTrail != null)
        {
            _myTrail.SetColor(_myColor);
            _myTrail.ResetTrail();
            _myTrail.gameObject.SetActive(true);
        }

        if (_originalSprite != null)
            _visuals.Setup(0, _originalSprite, _myColor, _defaultEmissionIntensity);

        gameObject.SetActive(true);
    }

    public void StartEngine()
    {
        _isRacing = true;
        _rb.linearVelocity = _currentDirection * _moveSpeed;
        if (_myTrail != null) _myTrail.gameObject.SetActive(true);
    }

    public void StopEngine()
    {
        _isRacing = false;
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    private void FixedUpdate()
    {
        if (!_isRacing) return;
        _rb.linearVelocity = _currentDirection * _moveSpeed;
        HandleObstacleAvoidance();
    }

    private void HandleObstacleAvoidance()
    {
        RaycastHit2D closestHit = default;
        float closestDistance = float.MaxValue;
        bool hasHit = false;

        LayerMask combinedLayer = _obstacleLayer | _interactableLayer;

        foreach (var dir in _checkDirections)
        {
            int count = Physics2D.RaycastNonAlloc(transform.position, dir, _rayHits, _rayDistance, combinedLayer);
            for (int i = 0; i < count; i++)
            {
                if (_rayHits[i].collider == _myCollider) continue;
                if (_rayHits[i].distance < closestDistance)
                {
                    closestDistance = _rayHits[i].distance;
                    closestHit = _rayHits[i];
                    hasHit = true;
                }
            }
        }

        if (hasHit && Vector2.Dot(_currentDirection, closestHit.normal) < 0)
        {
            CheckInteraction(closestHit.collider);
            ReflectMovement(closestHit);
        }
    }

    private void CheckInteraction(Collider2D hitCollider)
    {
        if (((1 << hitCollider.gameObject.layer) & _interactableLayer) != 0)
        {
            if (hitCollider.TryGetComponent<CountBumpObstacle>(out var bumpObstacle))
            {
                bumpObstacle.RegisterImpact(this);
            }
        }
    }

    private void ReflectMovement(RaycastHit2D hit)
    {
        _currentDirection = Vector2.Reflect(_currentDirection, hit.normal).normalized;
        float angle = Mathf.Atan2(_currentDirection.x, _currentDirection.y) * Mathf.Rad2Deg;
        _startAngle = -angle;
        _rb.linearVelocity = _currentDirection * _moveSpeed;

        PlayMusicalImpact(hit.point);

        if (EffectManager.Instance != null)
            EffectManager.Instance.PlayEffect(EffectType.WallImpact, hit.point, _myColor);
    }

    private void PlayMusicalImpact(Vector3 position)
    {
        if (MapAudioSequencer.Instance != null)
        {
            MapAudioSequencer.Instance.TryPlayNextHit(position);
        }
    }

    public void ConfigureVisuals(int id, Sprite sprite, Color color)
    {
        _myColor = color;
        _originalSprite = sprite;
        _visuals.Setup(id, sprite, color, _defaultEmissionIntensity);
        if (_myTrail != null) _myTrail.SetColor(color);
        name = $"Racer_{id:000}";
    }

    public void SetDeathSprite(Sprite sprite)
    {
        _fallbackDeadSprite = sprite;
    }

    public void TakeDamage(int amount)
    {
        if (_currentHealth <= 0) return;

        _currentHealth -= amount;

        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(DamageFlashRoutine());

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator DamageFlashRoutine()
    {
        _visuals.Setup(0, _originalSprite, Color.white, 10f);
        yield return new WaitForSeconds(0.05f);
        _visuals.Setup(0, _fallbackDeadSprite, _myColor, _defaultEmissionIntensity);
    }

    private void Die()
    {
        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.NotifyRacerDied(this);
        }
        TriggerDeactiveState();
    }

    public void TriggerDeactiveState()
    {
        StopEngine();
        _myCollider.enabled = false;

        var mount = GetComponentInChildren<WeaponMount>();
        if (mount != null) mount.ForceDrop();

        if (_myTrail != null) _myTrail.gameObject.SetActive(false);

        if (AudioManager.Instance != null && _deathSfx != null)
            AudioManager.Instance.PlaySFX(_deathSfx, transform.position, _sfxVolume);

        if (EffectManager.Instance != null)
            EffectManager.Instance.PlayEffect(EffectType.DeathExplosion, transform.position, _myColor);

        Sprite spriteToUse = _fallbackDeadSprite != null ? _fallbackDeadSprite : _originalSprite;
        _visuals.SetDeadState(spriteToUse, _myColor);

        if (!_isPermanentDeathBody) StartCoroutine(BodyDespawnRoutine());
    }

    private IEnumerator BodyDespawnRoutine()
    {
        yield return new WaitForSeconds(_bodyDespawnDelay);
        gameObject.SetActive(false);
    }

    public void EquipWeapon(GameObject prefab)
    {
        var mount = GetComponentInChildren<WeaponMount>();
        if (mount == null)
        {
            GameObject m = new GameObject("WeaponMount");
            m.transform.SetParent(transform);
            m.transform.localPosition = Vector3.zero;
            mount = m.AddComponent<WeaponMount>();
        }
        mount.Equip(prefab, _myCollider, transform);
    }

    public void ApplySettings(float speed, float rayDist, LayerMask mask)
    {
        _moveSpeed = speed;
        _rayDistance = rayDist;
        _obstacleLayer = mask;
    }

    public void SetInitialDirection(Vector2 dir)
    {
        _currentDirection = dir == Vector2.zero ? Vector2.up : dir.normalized;
        float angle = Mathf.Atan2(_currentDirection.x, _currentDirection.y) * Mathf.Rad2Deg;
        _startAngle = -angle;
    }

    [Header("Solid Death Config")]
    [SerializeField] private LayerMask _deadBodyLayer;
    [SerializeField] private PhysicsMaterial2D _deadBodyMaterial;

    public void DieAsSolidObstacle()
    {
        if (_currentHealth <= 0) return;
        _currentHealth = 0;

        if (RaceManager.Instance != null)
            RaceManager.Instance.NotifyRacerDied(this);

        StopEngine();
        _rb.linearVelocity = Vector2.zero;
        _rb.bodyType = RigidbodyType2D.Static;

        _myCollider.enabled = true;
        _myCollider.isTrigger = false;
        if (_deadBodyMaterial != null) _myCollider.sharedMaterial = _deadBodyMaterial;

        int layerIndex = (int)Mathf.Log(_deadBodyLayer.value, 2);
        gameObject.layer = layerIndex;

        var mount = GetComponentInChildren<WeaponMount>();
        if (mount != null) mount.ForceDrop();
        if (_myTrail != null) _myTrail.gameObject.SetActive(false);

        if (AudioManager.Instance != null && _deathSfx != null)
            AudioManager.Instance.PlaySFX(_deathSfx, transform.position, _sfxVolume);

        if (EffectManager.Instance != null)
            EffectManager.Instance.PlayEffect(EffectType.DeathExplosion, transform.position, _myColor);

        Sprite spriteToUse = _fallbackDeadSprite != null ? _fallbackDeadSprite : _originalSprite;
        _visuals.SetDeadState(spriteToUse, Color.Lerp(_myColor, Color.black, 0.3f));

        _myCollider.enabled = false;
    }

    public Color GetColor() => _myColor;
    public Sprite GetSprite() => _visuals.GetSprite();
    public Collider2D GetCollider() => _myCollider;
    public int GetHealth() => _currentHealth;
}