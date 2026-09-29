using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Prototype/CarSO", fileName = "CarSO")]
public class CarSO : ScriptableObject
{
    [Header("Car Prefabs")]
    public CarPrefabInfo[] carPrefabs;

    public GameObject GetPrefab(string carType)
    {
        if (!string.IsNullOrEmpty(carType))
        {
            foreach (var entry in carPrefabs)
            {
                if (entry.carType == carType)
                    return entry.prefab;
            }
        }

        return null;
    }

    public GameObject GetPrefab(CarType carType)
    {
        // Thu map theo index: Small=0, Medium=1, Big=2
        int index = carType switch
        {
            CarType.Small4Slots  => 0,
            CarType.Medium6Slots => 1,
            CarType.Big9Slots    => 2,
            _                    => 0
        };

        if (carPrefabs != null && index < carPrefabs.Length)
            return carPrefabs[index].prefab;

        return carPrefabs != null && carPrefabs.Length > 0 ? carPrefabs[0].prefab : null;
    }
}

[Serializable]
public struct CarPrefabInfo
{
    public string carType;
    public GameObject prefab;
}
