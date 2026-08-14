using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class VfxPoolable : MonoBehaviour
{
    [SerializeField] private VfxType type;
    private ParticleSystem particle;
    private Coroutine releaseRoutine;

    public const string POOL_KEY_SMOKE_DEFAULT = "vfx_smoke_default";
    public const string POOL_KEY_SMOKE_HURRY = "vfx_smoke_hurry";
    public const string POOL_KEY_BUS_HIT = "vfx_bus_hit";
    public const string POOL_KEY_BUS_RELEASE_BALL = "vfx_bus_release_ball";
    public const string POOL_KEY_BUS_SMALL_LANDING = "vfx_bus_small_landing";
    public const string POOL_KEY_BUS_MEDIUM_LANDING = "vfx_bus_medium_landing";
    public const string POOL_KEY_BUS_LARGE_LANDING = "vfx_bus_large_landing";
    public const string POOL_KEY_BOX_DIE = "vfx_box_die";
    public const string POOL_KEY_REVEAL_BUS = "vfx_reveal_bus";
    public const string POOL_KEY_STUCK_BUS = "vfx_stuck_bus";
    public const string POOL_KEY_ELECTRIC = "vfx_electric";
    public const string POOL_KEY_ICE_BREAK = "vfx_ice_break";

    public VfxType Type => type;

    private void Awake() { particle = GetComponent<ParticleSystem>(); }
    public void OnSpawned() { particle ??= GetComponent<ParticleSystem>(); particle.Clear(true); }
    public void OnDespawned() { Stop(true); }

    public void Release() { Release(0f); }
    public void Release(float delay)
    {
        if (releaseRoutine != null) StopCoroutine(releaseRoutine);
        releaseRoutine = delay <= 0f ? null : StartCoroutine(ReleaseDelay(delay));
        if (delay <= 0f) ObjectPool.Recycle(gameObject);
    }

    private IEnumerator ReleaseDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        releaseRoutine = null;
        ObjectPool.Recycle(gameObject);
    }

    public string GetPoolKey() => GetPoolKeyByType(type);

    public static string GetPoolKeyByType(VfxType vfxType)
    {
        return vfxType switch
        {
            VfxType.SmokeDefault => POOL_KEY_SMOKE_DEFAULT,
            VfxType.SmokeHurryBus => POOL_KEY_SMOKE_HURRY,
            VfxType.BusHit => POOL_KEY_BUS_HIT,
            VfxType.BusReleaseBall => POOL_KEY_BUS_RELEASE_BALL,
            VfxType.BusSmallLanding => POOL_KEY_BUS_SMALL_LANDING,
            VfxType.BusMediumLanding => POOL_KEY_BUS_MEDIUM_LANDING,
            VfxType.BusLargeLanding => POOL_KEY_BUS_LARGE_LANDING,
            VfxType.BoxDie => POOL_KEY_BOX_DIE,
            VfxType.RevealBus => POOL_KEY_REVEAL_BUS,
            VfxType.StuckBus => POOL_KEY_STUCK_BUS,
            VfxType.Electric => POOL_KEY_ELECTRIC,
            VfxType.IceBreak => POOL_KEY_ICE_BREAK,
            _ => string.Empty
        };
    }

    public static VfxType GetBusLandingVfx(BusType busType)
    {
        return busType switch
        {
            BusType.Small => VfxType.BusSmallLanding,
            BusType.Large => VfxType.BusLargeLanding,
            _ => VfxType.BusMediumLanding
        };
    }

    public static void PlayVfx(VfxType vfxType, Vector3 position) => PlayVfx(vfxType, position, Quaternion.identity);

    public static void PlayVfx(VfxType vfxType, Vector3 position, Quaternion rotation)
    {
        VfxPoolable prefab = FindPrefab(vfxType);
        if (prefab == null) return;
        VfxPoolable instance = ObjectPool.Spawn(prefab, position, rotation);
        instance.Play();
    }

    public void Play()
    {
        particle ??= GetComponent<ParticleSystem>();
        particle.Play(true);
    }

    public void Stop(bool clear)
    {
        if (particle == null) return;
        particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (clear) particle.Clear(true);
    }

    private static VfxPoolable FindPrefab(VfxType vfxType)
    {
        string path = "Vfx/" + GetPoolKeyByType(vfxType);
        return Resources.Load<VfxPoolable>(path);
    }
}
