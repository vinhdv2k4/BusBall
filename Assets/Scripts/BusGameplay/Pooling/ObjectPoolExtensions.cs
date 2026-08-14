using System.Collections.Generic;
using UnityEngine;

public static class ObjectPoolExtensions
{
    private class Pool
    {
        public readonly GameObject Prefab;
        public readonly List<GameObject> Pooled = new();
        public readonly List<GameObject> Spawned = new();

        public Pool(GameObject prefab) { Prefab = prefab; }
    }

    private static readonly Dictionary<GameObject, Pool> Pools = new();

    public static void CreatePool<T>(this T prefab) where T : Component => CreatePool(prefab, 0);
    public static void CreatePool<T>(this T prefab, int initialPoolSize) where T : Component => CreatePool(prefab.gameObject, initialPoolSize);
    public static void CreatePool(this GameObject prefab) => CreatePool(prefab, 0);

    public static void CreatePool(this GameObject prefab, int initialPoolSize)
    {
        if (prefab == null) return;
        Pool pool = GetOrCreatePool(prefab);
        for (int i = pool.Pooled.Count; i < Mathf.Max(0, initialPoolSize); i++)
            pool.Pooled.Add(CreateInactiveInstance(pool.Prefab));
    }

    public static T Spawn<T>(this T prefab, Transform parent, Vector3 position, Quaternion rotation) where T : Component
        => Spawn(prefab.gameObject, parent, position, rotation).GetComponent<T>();
    public static T Spawn<T>(this T prefab, Vector3 position, Quaternion rotation) where T : Component
        => Spawn(prefab, null, position, rotation);
    public static T Spawn<T>(this T prefab, Transform parent, Vector3 position) where T : Component
        => Spawn(prefab, parent, position, Quaternion.identity);
    public static T Spawn<T>(this T prefab, Vector3 position) where T : Component
        => Spawn(prefab, null, position, Quaternion.identity);
    public static T Spawn<T>(this T prefab, Transform parent) where T : Component
        => Spawn(prefab, parent, Vector3.zero, Quaternion.identity);
    public static T Spawn<T>(this T prefab) where T : Component
        => Spawn(prefab, null, Vector3.zero, Quaternion.identity);

    public static GameObject Spawn(this GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;
        Pool pool = GetOrCreatePool(prefab);
        GameObject instance = pool.Pooled.Count > 0 ? TakeLast(pool.Pooled) : CreateInactiveInstance(prefab);
        pool.Spawned.Add(instance);
        instance.transform.SetParent(parent, false);
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);
        return instance;
    }

    public static GameObject Spawn(this GameObject prefab, Vector3 position, Quaternion rotation) => Spawn(prefab, null, position, rotation);
    public static GameObject Spawn(this GameObject prefab, Transform parent, Vector3 position) => Spawn(prefab, parent, position, Quaternion.identity);
    public static GameObject Spawn(this GameObject prefab, Vector3 position) => Spawn(prefab, null, position, Quaternion.identity);
    public static GameObject Spawn(this GameObject prefab, Transform parent) => Spawn(prefab, parent, Vector3.zero, Quaternion.identity);
    public static GameObject Spawn(this GameObject prefab) => Spawn(prefab, null, Vector3.zero, Quaternion.identity);

    public static void Recycle<T>(this T obj) where T : Component => Recycle(obj == null ? null : obj.gameObject);
    public static void Recycle(this GameObject obj) => Recycle(obj, 0f);

    public static void Recycle(this GameObject obj, float delay)
    {
        if (obj == null) return;
        if (delay > 0f)
        {
            RecycleDelayRunner runner = obj.GetComponent<RecycleDelayRunner>();
            if (runner == null) runner = obj.AddComponent<RecycleDelayRunner>();
            runner.Begin(obj, delay);
            return;
        }
        RecycleNow(obj);
    }

    public static void RecycleAll<T>(this T prefab) where T : Component => RecycleAll(prefab.gameObject);
    public static void RecycleAll(this GameObject prefab)
    {
        if (!TryGetPool(prefab, out Pool pool)) return;
        foreach (GameObject instance in new List<GameObject>(pool.Spawned)) RecycleNow(instance);
    }

    public static int CountPooled<T>(this T prefab) where T : Component => CountPooled(prefab.gameObject);
    public static int CountPooled(this GameObject prefab) => TryGetPool(prefab, out Pool pool) ? pool.Pooled.Count : 0;
    public static int CountSpawned<T>(this T prefab) where T : Component => CountSpawned(prefab.gameObject);
    public static int CountSpawned(this GameObject prefab) => TryGetPool(prefab, out Pool pool) ? pool.Spawned.Count : 0;

    public static List<GameObject> GetSpawned(this GameObject prefab, List<GameObject> list, bool appendList)
        => CopyToList(TryGetPool(prefab, out Pool pool) ? pool.Spawned : null, list, appendList);
    public static List<GameObject> GetSpawned(this GameObject prefab, List<GameObject> list) => GetSpawned(prefab, list, false);
    public static List<GameObject> GetSpawned(this GameObject prefab) => GetSpawned(prefab, new List<GameObject>(), false);
    public static List<T> GetSpawned<T>(this T prefab, List<T> list, bool appendList) where T : Component
        => GetComponents(prefab.gameObject, list, appendList, true);
    public static List<T> GetSpawned<T>(this T prefab, List<T> list) where T : Component => GetSpawned(prefab, list, false);
    public static List<T> GetSpawned<T>(this T prefab) where T : Component => GetSpawned(prefab, new List<T>(), false);

    public static List<GameObject> GetPooled(this GameObject prefab, List<GameObject> list, bool appendList)
        => CopyToList(TryGetPool(prefab, out Pool pool) ? pool.Pooled : null, list, appendList);
    public static List<GameObject> GetPooled(this GameObject prefab, List<GameObject> list) => GetPooled(prefab, list, false);
    public static List<GameObject> GetPooled(this GameObject prefab) => GetPooled(prefab, new List<GameObject>(), false);
    public static List<T> GetPooled<T>(this T prefab, List<T> list, bool appendList) where T : Component
        => GetComponents(prefab.gameObject, list, appendList, false);
    public static List<T> GetPooled<T>(this T prefab, List<T> list) where T : Component => GetPooled(prefab, list, false);
    public static List<T> GetPooled<T>(this T prefab) where T : Component => GetPooled(prefab, new List<T>(), false);

    public static void DestroyPooled(this GameObject prefab) => DestroyPooledInternal(prefab);
    public static void DestroyPooled<T>(this T prefab) where T : Component => DestroyPooledInternal(prefab.gameObject);
    public static void DestroyAll(this GameObject prefab) => DestroyPooledInternal(prefab);
    public static void DestroyAll<T>(this T prefab) where T : Component => DestroyPooledInternal(prefab.gameObject);

    private static void RecycleNow(GameObject instance)
    {
        foreach (Pool pool in Pools.Values)
        {
            if (!pool.Spawned.Remove(instance)) continue;
            instance.SetActive(false);
            instance.transform.SetParent(null);
            pool.Pooled.Add(instance);
            return;
        }
    }

    private static void DestroyPooledInternal(GameObject prefab)
    {
        if (!TryGetPool(prefab, out Pool pool)) return;
        foreach (GameObject instance in pool.Pooled) if (instance != null) Object.Destroy(instance);
        foreach (GameObject instance in pool.Spawned) if (instance != null) Object.Destroy(instance);
        Pools.Remove(prefab);
    }

    private static Pool GetOrCreatePool(GameObject prefab)
    {
        if (!Pools.TryGetValue(prefab, out Pool pool)) Pools[prefab] = pool = new Pool(prefab);
        return pool;
    }

    private static bool TryGetPool(GameObject prefab, out Pool pool) => Pools.TryGetValue(prefab, out pool);
    private static GameObject CreateInactiveInstance(GameObject prefab) { GameObject instance = Object.Instantiate(prefab); instance.SetActive(false); return instance; }
    private static T TakeLast<T>(List<T> list) { int index = list.Count - 1; T item = list[index]; list.RemoveAt(index); return item; }
    private static List<GameObject> CopyToList(List<GameObject> source, List<GameObject> target, bool append) { if (target == null) target = new(); if (!append) target.Clear(); if (source != null) target.AddRange(source); return target; }

    private static List<T> GetComponents<T>(GameObject prefab, List<T> target, bool append, bool spawned) where T : Component
    {
        if (target == null) target = new List<T>();
        if (!append) target.Clear();
        if (!TryGetPool(prefab, out Pool pool)) return target;
        List<GameObject> source = spawned ? pool.Spawned : pool.Pooled;
        foreach (GameObject instance in source) if (instance != null && instance.TryGetComponent(out T component)) target.Add(component);
        return target;
    }

    private class RecycleDelayRunner : MonoBehaviour
    {
        public void Begin(GameObject target, float delay) { StartCoroutine(WaitAndRecycle(target, delay)); }
        private System.Collections.IEnumerator WaitAndRecycle(GameObject target, float delay)
        {
            yield return new WaitForSeconds(delay);
            target.Recycle();
            Destroy(this);
        }
    }
}
