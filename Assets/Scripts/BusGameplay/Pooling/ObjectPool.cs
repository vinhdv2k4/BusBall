using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public enum StartupPoolMode { Awake, Start, CallManually }

    [Serializable]
    public class StartupPool { public int size; public GameObject prefab; }

    private static ObjectPool _instance;
    private readonly Dictionary<GameObject, List<GameObject>> pooledObjects = new();
    private readonly Dictionary<GameObject, GameObject> spawnedObjects = new();
    private static readonly Dictionary<string, GameObject> pathToGameObjectDict = new();

    public StartupPoolMode startupPoolMode = StartupPoolMode.Awake;
    public StartupPool[] startupPools;
    private bool startupPoolsCreated;

    public static ObjectPool instance
    {
        get
        {
            if (_instance != null) return _instance;
            _instance = FindFirstObjectByType<ObjectPool>();
            if (_instance == null)
            {
                GameObject root = new("ObjectPool");
                _instance = root.AddComponent<ObjectPool>();
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        if (startupPoolMode == StartupPoolMode.Awake) CreateStartupPools();
    }

    private void Start()
    {
        if (startupPoolMode == StartupPoolMode.Start) CreateStartupPools();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    public static void CreateStartupPools()
    {
        if (instance.startupPoolsCreated) return;
        instance.startupPoolsCreated = true;
        if (instance.startupPools == null) return;
        foreach (StartupPool startupPool in instance.startupPools)
            if (startupPool?.prefab != null) CreatePool(startupPool.prefab, startupPool.size);
    }

    public static void CreatePool<T>(T prefab, int initialPoolSize) where T : Component => CreatePool(prefab?.gameObject, initialPoolSize);

    public static void CreatePool(GameObject prefab, int initialPoolSize)
    {
        if (prefab == null) return;
        ObjectPool pool = instance;
        if (!pool.pooledObjects.TryGetValue(prefab, out List<GameObject> objects))
            pool.pooledObjects[prefab] = objects = new List<GameObject>();
        while (objects.Count < Mathf.Max(0, initialPoolSize)) objects.Add(pool.CreateInstance(prefab));
    }

    public static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;
        ObjectPool pool = instance;
        if (!pool.pooledObjects.TryGetValue(prefab, out List<GameObject> objects))
            pool.pooledObjects[prefab] = objects = new List<GameObject>();
        GameObject result = objects.Count > 0 ? objects[^1] : pool.CreateInstance(prefab);
        if (objects.Count > 0) objects.RemoveAt(objects.Count - 1);
        result.transform.SetParent(parent, false);
        result.transform.SetPositionAndRotation(position, rotation);
        result.SetActive(true);
        pool.spawnedObjects[result] = prefab;
        return result;
    }

    public static T Spawn<T>(T prefab, Transform parent, Vector3 position, Quaternion rotation) where T : Component
        => Spawn(prefab?.gameObject, parent, position, rotation)?.GetComponent<T>();
    public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component => Spawn(prefab, null, position, rotation);
    public static T Spawn<T>(T prefab, Transform parent, Vector3 position) where T : Component => Spawn(prefab, parent, position, Quaternion.identity);
    public static T Spawn<T>(T prefab, Vector3 position) where T : Component => Spawn(prefab, null, position, Quaternion.identity);
    public static T Spawn<T>(T prefab, Transform parent) where T : Component => Spawn(prefab, parent, Vector3.zero, Quaternion.identity);
    public static T Spawn<T>(T prefab) where T : Component => Spawn(prefab, null, Vector3.zero, Quaternion.identity);
    public static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position) => Spawn(prefab, parent, position, Quaternion.identity);
    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation) => Spawn(prefab, null, position, rotation);
    public static GameObject Spawn(GameObject prefab, Transform parent) => Spawn(prefab, parent, Vector3.zero, Quaternion.identity);
    public static GameObject Spawn(GameObject prefab, Vector3 position) => Spawn(prefab, null, position, Quaternion.identity);
    public static GameObject Spawn(GameObject prefab) => Spawn(prefab, null, Vector3.zero, Quaternion.identity);

    public static GameObject Spawn(string path, Transform parent, Vector3 position, Quaternion rotation) => Spawn(GetGameObjectFromPath(path), parent, position, rotation);
    public static GameObject Spawn(string path, Transform parent, Vector3 position) => Spawn(path, parent, position, Quaternion.identity);
    public static GameObject Spawn(string path, Vector3 position, Quaternion rotation) => Spawn(path, null, position, rotation);
    public static GameObject Spawn(string path, Transform parent) => Spawn(path, parent, Vector3.zero, Quaternion.identity);
    public static GameObject Spawn(string path, Vector3 position) => Spawn(path, null, position, Quaternion.identity);
    public static GameObject Spawn(string path) => Spawn(path, null, Vector3.zero, Quaternion.identity);

    private static GameObject GetGameObjectFromPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (!pathToGameObjectDict.TryGetValue(path, out GameObject prefab))
            pathToGameObjectDict[path] = prefab = Resources.Load<GameObject>(path);
        return prefab;
    }

    public static void Recycle<T>(T obj) where T : Component => Recycle(obj?.gameObject);
    public static void Recycle(GameObject obj) => Recycle(obj, 0f);
    public static void Recycle(GameObject obj, float delay)
    {
        if (obj == null) return;
        if (delay > 0f) { instance.StartCoroutine(instance.RecycleAfter(obj, delay)); return; }
        ObjectPool pool = instance;
        if (!pool.spawnedObjects.TryGetValue(obj, out GameObject prefab)) return;
        pool.spawnedObjects.Remove(obj);
        obj.SetActive(false);
        obj.transform.SetParent(null);
        pool.pooledObjects[prefab].Add(obj);
    }

    private IEnumerator RecycleAfter(GameObject obj, float delay) { yield return new WaitForSeconds(delay); Recycle(obj); }
    public static void RecycleAll<T>(T prefab) where T : Component => RecycleAll(prefab?.gameObject);
    public static void RecycleAll(GameObject prefab) { foreach (GameObject obj in GetSpawned(prefab, new List<GameObject>(), false)) Recycle(obj); }
    public static void RecycleAll() { foreach (GameObject obj in new List<GameObject>(instance.spawnedObjects.Keys)) Recycle(obj); }
    public static bool IsSpawned(GameObject obj) => obj != null && instance.spawnedObjects.ContainsKey(obj);
    public static int CountPooled<T>(T prefab) where T : Component => CountPooled(prefab?.gameObject);
    public static int CountPooled(GameObject prefab) => instance.pooledObjects.TryGetValue(prefab, out List<GameObject> list) ? list.Count : 0;
    public static int CountSpawned<T>(T prefab) where T : Component => CountSpawned(prefab?.gameObject);
    public static int CountSpawned(GameObject prefab) { int count = 0; foreach (GameObject value in instance.spawnedObjects.Values) if (value == prefab) count++; return count; }
    public static int CountAllPooled() { int count = 0; foreach (List<GameObject> list in instance.pooledObjects.Values) count += list.Count; return count; }

    public static List<GameObject> GetPooled(GameObject prefab, List<GameObject> list, bool appendList) => Copy(instance.pooledObjects.TryGetValue(prefab, out List<GameObject> source) ? source : null, list, appendList);
    public static List<T> GetPooled<T>(T prefab, List<T> list, bool appendList) where T : Component => GetComponents(prefab?.gameObject, list, appendList, false);
    public static List<GameObject> GetSpawned(GameObject prefab, List<GameObject> list, bool appendList) { List<GameObject> source = new(); foreach (var pair in instance.spawnedObjects) if (pair.Value == prefab) source.Add(pair.Key); return Copy(source, list, appendList); }
    public static List<T> GetSpawned<T>(T prefab, List<T> list, bool appendList) where T : Component => GetComponents(prefab?.gameObject, list, appendList, true);

    public static void DestroyPooled(GameObject prefab) { if (!instance.pooledObjects.TryGetValue(prefab, out List<GameObject> list)) return; foreach (GameObject obj in list) if (obj != null) Destroy(obj); list.Clear(); }
    public static void DestroyPooled<T>(T prefab) where T : Component => DestroyPooled(prefab?.gameObject);
    public static void DestroyAll(GameObject prefab) { RecycleAll(prefab); DestroyPooled(prefab); }
    public static void DestroyAll<T>(T prefab) where T : Component => DestroyAll(prefab?.gameObject);

    private GameObject CreateInstance(GameObject prefab) { GameObject obj = Instantiate(prefab, transform); obj.SetActive(false); return obj; }
    private static List<GameObject> Copy(List<GameObject> source, List<GameObject> target, bool append) { target ??= new(); if (!append) target.Clear(); if (source != null) target.AddRange(source); return target; }
    private static List<T> GetComponents<T>(GameObject prefab, List<T> target, bool append, bool spawned) where T : Component { target ??= new(); if (!append) target.Clear(); foreach (GameObject obj in (spawned ? GetSpawned(prefab, new List<GameObject>(), false) : GetPooled(prefab, new List<GameObject>(), false))) if (obj.TryGetComponent(out T component)) target.Add(component); return target; }
}
