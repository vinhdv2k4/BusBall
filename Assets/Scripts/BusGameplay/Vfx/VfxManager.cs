using UnityEngine;

public class VfxManager : MonoBehaviour
{
    public static VfxManager Instance { get; private set; }

    [SerializeField] private VfxSO vfxSO;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public static void Play(VfxType type, Vector3 position)
    {
        Instance?.PlayInternal(type, position, Quaternion.identity);
    }

    public static void Play(VfxType type, Vector3 position, Quaternion rotation)
    {
        Instance?.PlayInternal(type, position, rotation);
    }

    public static VfxPoolable PlayAttached(VfxType type, Transform parent)
    {
        return Instance != null ? Instance.PlayAttachedInternal(type, parent) : null;
    }

    private void PlayInternal(VfxType type, Vector3 position, Quaternion rotation)
    {
        ParticleSystem prefab = vfxSO?.Get(type);
        if (prefab == null) return;

        // VFX instances are short-lived and can occur frequently during gameplay.
        // Reuse them through ObjectPool instead of allocating and destroying each hit.
        VfxPoolable poolablePrefab = prefab.GetComponent<VfxPoolable>();
        if (poolablePrefab != null)
        {
            VfxPoolable instance = ObjectPool.Spawn(poolablePrefab, position, rotation);
            instance.OnSpawned();
            instance.Play();
            instance.Release(GetLifetime(prefab));
            return;
        }

        ParticleSystem instanceParticle = ObjectPool.Spawn(prefab, position, rotation);
        instanceParticle.Clear(true);
        instanceParticle.Play(true);
        ObjectPool.Recycle(instanceParticle.gameObject, GetLifetime(prefab));
    }

    private static float GetLifetime(ParticleSystem particle)
    {
        return particle.main.duration + particle.main.startLifetime.constantMax;
    }

    private VfxPoolable PlayAttachedInternal(VfxType type, Transform parent)
    {
        if (parent == null) return null;

        ParticleSystem prefab = vfxSO?.Get(type);
        VfxPoolable poolablePrefab = prefab != null ? prefab.GetComponent<VfxPoolable>() : null;
        if (poolablePrefab == null) return null;

        VfxPoolable instance = ObjectPool.Spawn(poolablePrefab, parent, Vector3.zero, Quaternion.identity);
        if (instance == null || !instance.gameObject.scene.IsValid())
            return null;

        // Position is authored by the Bus marker, while the prefab's rotation
        // defines the particle emission direction and custom simulation space.
        instance.transform.SetLocalPositionAndRotation(Vector3.zero, poolablePrefab.transform.localRotation);
        instance.transform.localScale = poolablePrefab.transform.localScale;
        instance.OnSpawned();
        ParticleSystem particle = instance.GetComponent<ParticleSystem>();
        if (particle != null)
        {
            ParticleSystem.MainModule main = particle.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Custom;
            main.customSimulationSpace = parent;
        }
        instance.Play();
        return instance;
    }
}
