using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class PressObstacleVisuals : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float impactDuration = 0.15f;
    [SerializeField] private float heatDissolveSpeed = 2.0f;
    [SerializeField]
    private AnimationCurve squashCurve = new AnimationCurve(
        new Keyframe(0, 0),
        new Keyframe(0.3f, 0.3f),
        new Keyframe(1, 0)
    );

    private SpriteRenderer _spriteRenderer;
    private MaterialPropertyBlock _propBlock;
    private int _heatLevelID;
    private int _squashAmountID;
    private int _flashAmountID;
    private Coroutine _impactRoutine;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _propBlock = new MaterialPropertyBlock();

        _heatLevelID = Shader.PropertyToID("_HeatLevel");
        _squashAmountID = Shader.PropertyToID("_SquashAmount");
        _flashAmountID = Shader.PropertyToID("_FlashAmount");
    }

    public void TriggerImpact()
    {
        if (_impactRoutine != null) StopCoroutine(_impactRoutine);
        _impactRoutine = StartCoroutine(ProcessImpactEffect());
    }

    private IEnumerator ProcessImpactEffect()
    {
        float elapsed = 0f;

        while (elapsed < impactDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / impactDuration;

            float currentSquash = squashCurve.Evaluate(progress);
            float currentFlash = 1.0f - progress;
            float currentHeat = Mathf.Lerp(0.8f, 1.0f, progress);

            _spriteRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(_squashAmountID, currentSquash);
            _propBlock.SetFloat(_flashAmountID, currentFlash);
            _propBlock.SetFloat(_heatLevelID, currentHeat);
            _spriteRenderer.SetPropertyBlock(_propBlock);

            yield return null;
        }

        StartCoroutine(DissolveHeat());
    }

    private IEnumerator DissolveHeat()
    {
        _spriteRenderer.GetPropertyBlock(_propBlock);
        float currentHeat = _propBlock.GetFloat(_heatLevelID);

        while (currentHeat > 0)
        {
            currentHeat -= Time.deltaTime * heatDissolveSpeed;

            _spriteRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(_heatLevelID, Mathf.Max(0, currentHeat));
            _propBlock.SetFloat(_squashAmountID, 0);
            _propBlock.SetFloat(_flashAmountID, 0);
            _spriteRenderer.SetPropertyBlock(_propBlock);

            yield return null;
        }
    }
}