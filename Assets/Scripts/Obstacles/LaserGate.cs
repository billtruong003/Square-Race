using UnityEngine;
using System.Collections;
using Sirenix.OdinInspector;

public class LaserGate : MonoBehaviour
{
    [BoxGroup("Setup")]
    [SerializeField] private LineRenderer _laserRenderer;
    [BoxGroup("Setup")]
    [SerializeField] private Transform _startPoint;
    [BoxGroup("Setup")]
    [SerializeField] private Transform _endPoint;
    [BoxGroup("Setup")]
    [SerializeField] private BoxCollider2D _killTrigger;

    [BoxGroup("Timing")]
    [SerializeField] private float _activeDuration = 2f;
    [BoxGroup("Timing")]
    [SerializeField] private float _inactiveDuration = 2f;
    [BoxGroup("Timing")]
    [SerializeField] private float _startOffset = 0f;
    [BoxGroup("Timing")]
    [SerializeField] private float _warningTime = 0.5f;

    [BoxGroup("Damage")]
    [SerializeField] private int _damage = 9999;
    [BoxGroup("Damage")]
    [SerializeField] private LayerMask _targetLayer;

    private Coroutine _routine;

    private void Awake()
    {
        UpdatePositions();
        _killTrigger.isTrigger = true;
        _killTrigger.enabled = false;
        SetLaserVisual(false, false);
    }

    private void Start()
    {
        _routine = StartCoroutine(CycleRoutine());
    }

    private void UpdatePositions()
    {
        if (_startPoint != null && _endPoint != null)
        {
            _laserRenderer.SetPosition(0, _startPoint.position);
            _laserRenderer.SetPosition(1, _endPoint.position);

            // Align Trigger to Line
            float dist = Vector2.Distance(_startPoint.position, _endPoint.position);
            Vector2 mid = (_startPoint.position + _endPoint.position) / 2f;
            float angle = Mathf.Atan2(_endPoint.position.y - _startPoint.position.y, _endPoint.position.x - _startPoint.position.x) * Mathf.Rad2Deg;

            _killTrigger.transform.position = mid;
            _killTrigger.transform.rotation = Quaternion.Euler(0, 0, angle);
            _killTrigger.size = new Vector2(dist, 0.5f); // Width of laser trigger
        }
    }

    private IEnumerator CycleRoutine()
    {
        if (_startOffset > 0)
        {
            SetLaserVisual(false, false);
            yield return new WaitForSeconds(_startOffset);
        }

        while (true)
        {
            // State: INACTIVE
            _killTrigger.enabled = false;
            SetLaserVisual(false, false);
            yield return new WaitForSeconds(_inactiveDuration - _warningTime);

            // State: WARNING (Flicker or Thin line)
            SetLaserVisual(true, true);
            yield return new WaitForSeconds(_warningTime);

            // State: ACTIVE (KILL)
            _killTrigger.enabled = true;
            SetLaserVisual(true, false);
            yield return new WaitForSeconds(_activeDuration);
        }
    }

    private void SetLaserVisual(bool active, bool isWarning)
    {
        _laserRenderer.enabled = active;

        if (active)
        {
            if (isWarning)
            {
                _laserRenderer.startWidth = 0.05f;
                _laserRenderer.endWidth = 0.05f;
                _laserRenderer.startColor = new Color(1, 0, 0, 0.3f);
                _laserRenderer.endColor = new Color(1, 0, 0, 0.3f);
            }
            else
            {
                _laserRenderer.startWidth = 0.4f;
                _laserRenderer.endWidth = 0.4f;
                _laserRenderer.startColor = Color.red;
                _laserRenderer.endColor = Color.white; // Core bright
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & _targetLayer) != 0)
        {
            if (other.TryGetComponent<IDamageable>(out var victim))
            {
                victim.TakeDamage(_damage);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (_startPoint != null && _endPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(_startPoint.position, _endPoint.position);
        }
    }
}