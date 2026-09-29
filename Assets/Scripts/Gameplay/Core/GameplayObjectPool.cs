using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class GameplayObjectPool : MonoBehaviour
{
    public static GameplayObjectPool Instance { get; private set; }
    [Header("Pool Roots")]
    [SerializeField] private Transform trayPoolRoot;
    [SerializeField] private Transform carPoolRoot;
    [SerializeField] private Transform ballPoolRoot;
    private GameObject ballPrefab;
    [Header("Runtime Stats")]
    [SerializeField] private int pooledBallCount;
    [SerializeField] private int pooledCarCount;
    private readonly Queue<GameObject> _balls = new();
    private readonly Dictionary<GameObject, Queue<GameObject>> _cars = new();
    private readonly HashSet<int> _pooledBallIds = new();
    private readonly HashSet<int> _pooledCarIds = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (trayPoolRoot == null) trayPoolRoot = CreateRoot("TrayPool");
        if (carPoolRoot == null) carPoolRoot = CreateRoot("CarPool");
        if (ballPoolRoot == null) ballPoolRoot = CreateRoot("BallPool");
    }

    private Transform CreateRoot(string rootName)
    {
        var root = new GameObject(rootName).transform;
        root.SetParent(transform, false);
        return root;
    }
    public void Configure(GameObject prefab) { if (prefab != null) ballPrefab = prefab; }
    public GameObject GetBall(Vector3 pos, Quaternion rot, Transform parent = null)
    {
        if (ballPrefab == null) return null;
        GameObject ball = null;
        while (_balls.Count > 0 && ball == null)
            ball = _balls.Dequeue();
        if (ball == null)
            ball = Instantiate(ballPrefab);
        _pooledBallIds.Remove(ball.GetInstanceID());
        pooledBallCount = _balls.Count;

        // Giữ bóng ở root hoặc parent sạch để Rigidbody hoàn toàn độc lập trong world space
        ball.transform.SetParent(parent != null ? parent : ballPoolRoot, true);
        ball.transform.SetPositionAndRotation(pos, rot);
        ball.transform.localScale = Vector3.one;

        var rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        // Enable all colliders on the ball since they might have been disabled on the conveyor
        var colliders = ball.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = true;
            colliders[i].isTrigger = false;
        }

        ball.SetActive(true);
        return ball;
    }
    public void Release(GameObject ball)
    {
        if (ball == null) return;
        if (!_pooledBallIds.Add(ball.GetInstanceID())) return;
        ball.transform.DOKill();
        var rb = ball.GetComponent<Rigidbody>();
        if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.isKinematic = true; rb.useGravity = false; }
        ball.SetActive(false);
        ball.transform.SetParent(ballPoolRoot, false);
        _balls.Enqueue(ball);
        pooledBallCount = _balls.Count;
    }

    public GameObject GetCar(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (prefab == null) return null;
        if (!_cars.TryGetValue(prefab, out var queue))
            _cars[prefab] = queue = new Queue<GameObject>();
        GameObject car = null;
        while (queue.Count > 0 && car == null)
            car = queue.Dequeue();
        if (car == null)
            car = Instantiate(prefab);
        _pooledCarIds.Remove(car.GetInstanceID());
        pooledCarCount = GetTotalPooledCarCount();
        car.transform.SetParent(parent, false);
        car.transform.SetPositionAndRotation(position, rotation);
        car.SetActive(true);
        return car;
    }

    public void ReleaseCar(GameObject car, GameObject prefab)
    {
        if (car == null || prefab == null) return;
        if (!_pooledCarIds.Add(car.GetInstanceID())) return;
        if (!_cars.TryGetValue(prefab, out var queue))
            _cars[prefab] = queue = new Queue<GameObject>();
        car.transform.DOKill();
        car.SetActive(false);
        car.transform.SetParent(carPoolRoot, false);
        queue.Enqueue(car);
        pooledCarCount = GetTotalPooledCarCount();
    }

    private int GetTotalPooledCarCount()
    {
        int count = 0;
        foreach (var pair in _cars)
            count += pair.Value.Count;
        return count;
    }
}
