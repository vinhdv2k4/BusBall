using System;
using UnityEngine;

public class LevelJsonLoader : MonoBehaviour
{
    [SerializeField] private TextAsset levelJson;
    [SerializeField] private PrefabSO prefabSO;
    [SerializeField] private TopGameController topGameController;
    [SerializeField, Min(0)] private int conveyorSlotCapacity = 9;
    [SerializeField] private bool loadOnAwake = true;
    [SerializeField] private bool startBusesImmediately;

    public LevelJsonData CurrentLevel { get; private set; }
    private Level level;

    private void Awake()
    {
        level = GetComponent<Level>();
        if (loadOnAwake) LoadLevel();
    }

    public bool LoadLevel()
    {
        if (CurrentLevel != null) return true;
        if (levelJson == null)
        {
            Debug.LogError("LevelJsonLoader cần levelJson.", this);
            return false;
        }

        LevelJsonData parsed;
        try
        {
            parsed = JsonUtility.FromJson<LevelJsonData>(levelJson.text);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            return false;
        }

        if (parsed == null || parsed.busEnrichedDatas == null)
        {
            Debug.LogError("JSON level không có busEnrichedDatas hợp lệ.", this);
            return false;
        }

        CurrentLevel = parsed;
        topGameController?.Init(parsed.topGameConfig, conveyorSlotCapacity);
        for (int i = 0; i < parsed.busEnrichedDatas.Length; i++)
        {
            BusOutObjectEnrichedData entry = parsed.busEnrichedDatas[i];
            if (entry == null || entry.Type != BusOutObjectType.Bus || entry.BusData == null) continue;

            Bus prefab = prefabSO?.GetBus(entry.BusData.busType);
            if (prefab == null)
            {
                Debug.LogError($"Không có Bus prefab cho BusType {entry.BusData.busType}.", this);
                continue;
            }

            Transform objectRoot = level != null ? level.ObjectRoot : transform;
            Bus bus = Instantiate(prefab, entry.Position, Quaternion.Euler(0f, entry.Rotation, 0f), objectRoot);
            bus.Configure(entry.BusData, entry.Position, entry.Rotation);
            if (startBusesImmediately) bus.StartMoveStraight();
        }
        return true;
    }
}
