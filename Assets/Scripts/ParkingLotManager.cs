using System.Collections.Generic;
using UnityEngine;

public class ParkingLotManager : MonoBehaviour
{
    [Header("Spawn")]
    public CarSO carSO;
    public Transform carParent;
    public bool autoSpawnOnStart = true;
    public int startLevel = 1;

    private readonly List<Car> _cars = new List<Car>();

    public int CarCount => _cars.Count;

    private void Start()
    {
        // LevelManager la noi khoi tao level chinh. Tranh spawn xe hai lan
        // trong cung frame khi ca hai component deu bat auto load.
        if (autoSpawnOnStart
            && FindAnyObjectByType<BallDropParty.Gameplay.LevelManager>(FindObjectsInactive.Include) == null)
        {
            InitCars(startLevel);
        }
    }

    public void InitCars(int level)
    {
        InitCars(level, DataManager.LoadLevel(level));
    }

    public void InitCars(int level, LevelData levelData)
    {
        Debug.Log($"ParkingLotManager: InitCars(level={level})");

        ClearSpawnedCars();

        if (carSO == null)
        {
            Debug.LogWarning("ParkingLotManager: carSO is not assigned.");
            return;
        }

        var cars = levelData?.GetCars();
        if (cars == null || cars.Length == 0)
        {
            Debug.LogWarning($"ParkingLotManager: Loaded level {level}, but no cars were found.");
            return;
        }

        Debug.Log($"ParkingLotManager: loaded {cars.Length} car(s) from level {level}");
        foreach (var carData in cars)
        {
            Debug.Log($"ParkingLotManager: carType='{carData.carType}', size={carData.size}, color={carData.eColor}");

            var prefab = carSO.GetPrefab(carData.carType);
            if (prefab == null)
                prefab = carSO.GetPrefab(carData.type);

            if (prefab == null)
            {
                Debug.LogWarning($"ParkingLotManager: No prefab found for carType='{carData.carType}' size={carData.size}. Available: {string.Join(", ", System.Array.ConvertAll(carSO.carPrefabs, p => p.carType))}");
                continue;
            }

            Vector3 spawnPosition = ResolveSpawnPosition(carData);
            var instance = GameplayObjectPool.Instance != null
                ? GameplayObjectPool.Instance.GetCar(prefab, spawnPosition, Quaternion.identity, carParent)
                : Instantiate(prefab, spawnPosition, Quaternion.identity, carParent);

            var car = instance.GetComponent<Car>();
            if (car == null)
            {
                Debug.LogWarning("ParkingLotManager: car prefab is missing a Car component.");
                continue;
            }

            car.Init(carData, carSO);
            _cars.Add(car);
        }
    }

    private Vector3 ResolveSpawnPosition(CarData carData)
    {
        if (carData == null)
            return new Vector3(0f, 0f, 0.5f);

        // 1. Neu co position (toa do thuc)
        if (carData.position != Vector2.zero)
        {
            return new Vector3(carData.position.x, carData.position.y, 0.5f);
        }

        // 2. Neu co gridPosition (x,y) -> dung truc tiep toa do x, y với Z = 0.5f
        if (carData.gridPosition != null)
        {
            return new Vector3(carData.gridPosition.x, carData.gridPosition.y, 0.5f);
        }

        return new Vector3(0f, 0f, 0.5f);
    }

    private void ClearSpawnedCars()
    {
        for (int i = _cars.Count - 1; i >= 0; i--)
        {
            if (_cars[i] != null)
            {
                if (GameplayObjectPool.Instance != null)
                    GameplayObjectPool.Instance.ReleaseCar(_cars[i].gameObject, carSO.GetPrefab(_cars[i].CarData.type));
                else
                    Destroy(_cars[i].gameObject);
            }
        }

        _cars.Clear();
    }

    public void ReleaseCarToPool(Car car)
    {
        if (car == null) return;
        _cars.Remove(car);

        GameObject prefab = GetPrefabForCar(car);
        if (GameplayObjectPool.Instance != null && prefab != null)
            GameplayObjectPool.Instance.ReleaseCar(car.gameObject, prefab);
        else
            Destroy(car.gameObject);
    }

    public GameObject GetPrefabForCar(Car car)
    {
        return car != null && car.CarData != null && carSO != null
            ? carSO.GetPrefab(car.CarData.type)
            : null;
    }
}
