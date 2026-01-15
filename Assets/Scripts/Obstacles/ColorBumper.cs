using UnityEngine;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Collider2D))]
public class ColorBumper : MonoBehaviour
{
    [BoxGroup("Config")]
    [SerializeField] private Color _requiredColor = Color.red;
    [BoxGroup("Config")]
    [SerializeField] private SpriteRenderer _renderer;

    [BoxGroup("Physics")]
    [SerializeField] private float _bounceForce = 20f;
    [BoxGroup("Physics")]
    [SerializeField] private int _punishDamage = 30;

    [BoxGroup("Audio")]
    [SerializeField] private AudioClip _correctSfx;
    [BoxGroup("Audio")]
    [SerializeField] private AudioClip _wrongSfx;

    [Button("Apply Color To Sprite"), GUIColor(0, 1, 1)]
    private void ApplyVisualColor()
    {
        if (_renderer != null)
        {
            _renderer.color = _requiredColor;
        }
    }

    private void OnValidate() => ApplyVisualColor();

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.TryGetComponent<SquareController>(out var racer))
        {
            bool isColorMatch = IsColorSimilar(racer.GetColor(), _requiredColor);

            HandleBounce(other.rigidbody, other.contacts[0].normal);

            if (isColorMatch)
            {
                OnCorrectHit();
            }
            else
            {
                OnWrongHit(racer);
            }
        }
    }

    private void HandleBounce(Rigidbody2D rb, Vector2 normal)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(-normal * _bounceForce, ForceMode2D.Impulse);
        }
    }

    private void OnCorrectHit()
    {
        if (AudioManager.Instance != null && _correctSfx != null)
            AudioManager.Instance.PlaySFX(_correctSfx, transform.position);

        // Optional: Play Happy VFX
    }

    private void OnWrongHit(SquareController racer)
    {
        racer.TakeDamage(_punishDamage);

        if (AudioManager.Instance != null && _wrongSfx != null)
            AudioManager.Instance.PlaySFX(_wrongSfx, transform.position);

        // Optional: Play Angry VFX
    }

    private bool IsColorSimilar(Color a, Color b, float tolerance = 0.1f)
    {
        float diff = Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
        return diff < tolerance;
    }
}