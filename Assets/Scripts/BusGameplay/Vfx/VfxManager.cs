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

    private void PlayInternal(VfxType type, Vector3 position, Quaternion rotation)
    {
        ParticleSystem prefab = vfxSO?.Get(type);
        if (prefab == null) return;

        ParticleSystem instance = Instantiate(prefab, position, rotation);
        instance.Play(true);
        float lifetime = instance.main.duration + instance.main.startLifetime.constantMax;
        Destroy(instance.gameObject, lifetime);
    }
}
