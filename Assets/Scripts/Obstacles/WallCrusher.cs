using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Collider2D))]
public class WallCrusher : MonoBehaviour
{
    [BoxGroup("Config")]
    [SerializeField]
    [Tooltip("Layer của Player hoặc các object có thể bị ép chết")]
    private LayerMask _targetLayer;

    [BoxGroup("Config")]
    [SerializeField] private int _damageAmount = 9999;

    [BoxGroup("Debug")]
    [ShowInInspector, ReadOnly]
    private bool _isCrushing = false;

    private Collider2D _myCollider;
    private ContactFilter2D _filter;
    private readonly List<Collider2D> _results = new List<Collider2D>(5);

    private void Awake()
    {
        _myCollider = GetComponent<Collider2D>();

        // Tối ưu hóa: Chỉ check va chạm với đúng Layer Player
        _filter = new ContactFilter2D
        {
            useTriggers = true, // Player thường có trigger ở body hoặc collider thường đều tính
            useLayerMask = true,
            layerMask = _targetLayer
        };
    }

    private void FixedUpdate()
    {
        // Logic: Hỏi Physics Engine xem có ai đang "nằm trong" collider của tường không
        // OverlapCollider trả về số lượng collider đang lồng vào nhau
        int count = _myCollider.Overlap(_filter, _results);

        _isCrushing = count > 0;

        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                // Bỏ qua chính bản thân tường (dù đã filter layer nhưng check cho chắc)
                if (_results[i] == _myCollider) continue;

                // Tìm component IDamageable từ object bị kẹt (hoặc cha của nó)
                var victim = _results[i].GetComponentInParent<IDamageable>();

                if (victim != null)
                {
                    victim.TakeDamage(_damageAmount);

                    // Optional: Play effect máu me tung tóe tại vị trí kẹt
                    // EffectManager.Instance.PlayEffect(...)
                }
            }
        }
    }
}