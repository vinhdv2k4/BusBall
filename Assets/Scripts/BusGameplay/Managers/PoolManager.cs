using UnityEngine;

public class PoolManager : MonoBehaviour
{
    [SerializeField] private Transform poolRoot;
    [SerializeField] private ObjectPool.StartupPool[] startupPools;

    private void Awake()
    {
        ObjectPool pool = ObjectPool.instance;
        if (poolRoot != null) pool.transform.SetParent(poolRoot, false);
        if (startupPools == null) return;
        foreach (ObjectPool.StartupPool startupPool in startupPools)
            if (startupPool?.prefab != null) ObjectPool.CreatePool(startupPool.prefab, startupPool.size);
    }
}
