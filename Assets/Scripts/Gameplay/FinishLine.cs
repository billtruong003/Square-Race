using UnityEngine;

public class FinishLine : MonoBehaviour
{
    [SerializeField] private AudioClip _finishSfx;
    [Range(0f, 1f)][SerializeField] private float _volume = 1f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<SquareController>(out var racer)) return;

        PlayFeedback(racer);

        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.NotifyRacerFinished(racer);
        }
    }

    private void PlayFeedback(SquareController racer)
    {
        if (AudioManager.Instance != null && _finishSfx != null)
        {
            AudioManager.Instance.PlaySFX(_finishSfx, racer.transform.position, _volume);
        }

        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.PlayEffect(EffectType.FinishExplosion, racer.transform.position, racer.GetColor());
        }
    }
}