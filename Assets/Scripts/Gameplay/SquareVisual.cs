using UnityEngine;
using TMPro;

[RequireComponent(typeof(SpriteRenderer))]
public class SquareVisual : MonoBehaviour
{
    [SerializeField] private TextMeshPro _numberText;
    [SerializeField] private SpriteRenderer _renderer;

    private MaterialPropertyBlock _propBlock;

    private void Awake()
    {
        if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    public void Setup(int id, Sprite sprite, Color color, float intensity)
    {
        if (_numberText != null)
        {
            _numberText.text = id.ToString();
            _numberText.sortingOrder = _renderer.sortingOrder + 1;
            _numberText.gameObject.SetActive(true);
        }

        if (sprite != null) _renderer.sprite = sprite;

        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(GameConstants.BaseColorId, color);
        _propBlock.SetFloat(GameConstants.EmissionIntensityId, intensity);
        _renderer.SetPropertyBlock(_propBlock);
    }

    public void SetDeadState(Sprite deadSprite, Color color)
    {
        if (_numberText != null) _numberText.gameObject.SetActive(false);

        if (deadSprite != null) _renderer.sprite = deadSprite;

        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(GameConstants.BaseColorId, color * 0.5f);
        _propBlock.SetFloat(GameConstants.EmissionIntensityId, 0f);
        _renderer.SetPropertyBlock(_propBlock);
    }

    public Color GetBaseColor()
    {
        _renderer.GetPropertyBlock(_propBlock);
        return _propBlock.GetColor(GameConstants.BaseColorId);
    }

    public Sprite GetSprite() => _renderer.sprite;
}