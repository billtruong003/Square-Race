using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum EffectType
{
    FinishExplosion,
    DeathExplosion,
    WallImpact
}

[System.Serializable]
public class EffectPoolConfig
{
    public EffectType Type;
    public ParticleSystem Prefab;
    public int PoolSize = 10;
}

public class EffectManager : Singleton<EffectManager>
{
    [SerializeField] private List<EffectPoolConfig> _configs;
    private readonly Dictionary<EffectType, Queue<ParticleSystem>> _pools = new Dictionary<EffectType, Queue<ParticleSystem>>();

    protected override void Awake()
    {
        base.Awake();
        InitializePools();
    }

    private void InitializePools()
    {
        GameObject root = new GameObject("VFX_Pools");
        root.transform.SetParent(transform);

        foreach (var config in _configs)
        {
            _pools[config.Type] = new Queue<ParticleSystem>();
            for (int i = 0; i < config.PoolSize; i++)
            {
                CreatePoolItem(config, root.transform);
            }
        }
    }

    private void CreatePoolItem(EffectPoolConfig config, Transform parent)
    {
        ParticleSystem vfx = Instantiate(config.Prefab, parent);
        vfx.gameObject.SetActive(false);
        _pools[config.Type].Enqueue(vfx);
    }

    public void PlayEffect(EffectType type, Vector3 position, Color color)
    {
        if (!_pools.TryGetValue(type, out var pool) || pool.Count == 0) return;

        ParticleSystem vfx = pool.Dequeue();
        vfx.transform.position = position;
        vfx.gameObject.SetActive(true);

        SetEffectColor(vfx, color);
        vfx.Play();

        StartCoroutine(ReturnToPool(type, vfx));
    }

    private void SetEffectColor(ParticleSystem vfx, Color color)
    {
        var main = vfx.main;
        main.startColor = color;

        var trails = vfx.trails;
        if (trails.enabled) trails.colorOverLifetime = color;
    }

    private IEnumerator ReturnToPool(EffectType type, ParticleSystem vfx)
    {
        yield return new WaitForSeconds(2f);
        vfx.gameObject.SetActive(false);
        _pools[type].Enqueue(vfx);
    }
}