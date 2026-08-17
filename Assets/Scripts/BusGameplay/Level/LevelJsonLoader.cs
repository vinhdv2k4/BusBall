using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelJsonLoader : MonoBehaviour
{
    private const string CurrentLevelKey = "BusBall.CurrentLevel";

    [SerializeField] private TextAsset levelJson;
    [SerializeField] private PrefabSO prefabSO;
    [SerializeField] private TopGameController topGameController;
    [SerializeField] private bool loadOnAwake = true;
    [SerializeField] private bool startBusesImmediately;
    [Header("Debug")]
    [SerializeField] private bool createNearPointMarkers = true;
    [SerializeField, Min(0.01f)] private float nearPointMarkerSize = 0.14f;

    public LevelJsonData CurrentLevel { get; private set; }
    public int CurrentLevelIndex { get; private set; }
    private Level level;

    private void Awake()
    {
        level = GetComponent<Level>();
        int defaultLevelIndex = GetDefaultLevelIndex();
        int savedLevelIndex = PlayerPrefs.GetInt(CurrentLevelKey, defaultLevelIndex);
        if (!TrySetLevelJson(savedLevelIndex))
            TrySetLevelJson(defaultLevelIndex);
        if (loadOnAwake) LoadLevel();
    }

    public void SelectNextLevel()
    {
        int nextLevelIndex = CurrentLevelIndex + 1;
        if (!HasLevel(nextLevelIndex)) nextLevelIndex = 1;

        PlayerPrefs.SetInt(CurrentLevelKey, nextLevelIndex);
        PlayerPrefs.Save();
    }

    public bool LoadLevel()
    {
        if (CurrentLevel != null) return true;
        if (levelJson == null)
        {
            Debug.LogError("LevelJsonLoader cáº§n levelJson.", this);
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
            Debug.LogError("JSON level khÃ´ng cÃ³ busEnrichedDatas há»£p lá»‡.", this);
            return false;
        }

        CurrentLevel = parsed;
        float objectScale = parsed.objectScale > 0.01f ? parsed.objectScale : 1f;
        if (level != null && level.ObjectRoot != null)
            level.ObjectRoot.localScale = Vector3.one;
        topGameController?.Init(parsed.topGameConfig, prefabSO);
        PrewarmGameplayPools(parsed);
        List<Bus> spawnedBuses = new();
        Dictionary<int, Bus> busesByLevelIndex = new();
        Transform nearPointMarkerRoot = CreateNearPointMarkerRoot();
        RoadPath roadPath = FindFirstObjectByType<RoadPath>();
        for (int i = 0; i < parsed.busEnrichedDatas.Length; i++)
        {
            BusOutObjectEnrichedData entry = parsed.busEnrichedDatas[i];
            if (entry == null || entry.Type != BusOutObjectType.Bus || entry.BusData == null) continue;

            Bus prefab = prefabSO?.GetBus(entry.BusData.busType);
            if (prefab == null)
            {
                Debug.LogError($"KhÃ´ng cÃ³ Bus prefab cho BusType {entry.BusData.busType}.", this);
                continue;
            }

            Transform objectRoot = level != null ? level.ObjectRoot : transform;
            Bus bus = ObjectPool.Spawn(prefab, objectRoot, entry.Position,
                Quaternion.Euler(0f, 0f, entry.Rotation));
            bus.transform.localScale = prefab.transform.localScale * objectScale;
            bus.Configure(entry.BusData, entry.AnalysisData, entry.BusPathData,
                entry.Position, entry.Rotation);
            bus.SetBallPrefab(prefabSO.GetBall());
            bus.SetLevelIndex(i);
            spawnedBuses.Add(bus);
            busesByLevelIndex[i] = bus;
            CreateNearPointMarker(nearPointMarkerRoot, roadPath, entry, i);
        }

        topGameController?.SetExpectedBusReleaseCount(spawnedBuses.Count);

        foreach (Bus bus in spawnedBuses)
            bus.ClearBlockingBuses();

        foreach (Bus sourceBus in spawnedBuses)
        {
            foreach (int blockedIndex in sourceBus.AnalysisData.GetBlockedBusIndices())
            {
                if (busesByLevelIndex.TryGetValue(blockedIndex, out Bus blockedBus) &&
                    blockedBus != sourceBus)
                {
                    sourceBus.AddBlockedBus(blockedBus);
                    blockedBus.AddBlockingBus(sourceBus);
                }
            }
        }

        if (startBusesImmediately)
            foreach (Bus bus in spawnedBuses)
                bus.StartMoveStraight();

        return true;
    }

    private Transform CreateNearPointMarkerRoot()
    {
        if (!createNearPointMarkers) return null;

        Transform objectRoot = level != null && level.ObjectRoot != null ? level.ObjectRoot : transform;
        GameObject markerRoot = new("Near Point Markers");
        markerRoot.transform.SetParent(objectRoot, false);
        return markerRoot.transform;
    }

    private void CreateNearPointMarker(Transform markerRoot, RoadPath roadPath,
        BusOutObjectEnrichedData entry, int busIndex)
    {
        if (markerRoot == null || roadPath == null || entry?.BusPathData == null ||
            !roadPath.TryGetRoad(entry.BusPathData.splineContainerIndex, out var road))
            return;

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = $"Bus {busIndex} - Near Point";
        marker.transform.SetParent(markerRoot, false);
        marker.transform.position = road.transform.TransformPoint(entry.BusPathData.nearestPoint);
        marker.transform.localScale = Vector3.one * nearPointMarkerSize;

        Collider markerCollider = marker.GetComponent<Collider>();
        if (markerCollider != null)
            markerCollider.enabled = false;

        Renderer markerRenderer = marker.GetComponent<Renderer>();
        if (markerRenderer != null)
            markerRenderer.material.color = Color.magenta;
    }

    private void PrewarmGameplayPools(LevelJsonData parsed)
    {
        if (prefabSO == null || parsed.busEnrichedDatas == null) return;

        Dictionary<Bus, int> busCounts = new();
        int ballCount = 0;
        foreach (BusOutObjectEnrichedData entry in parsed.busEnrichedDatas)
        {
            if (entry == null || entry.Type != BusOutObjectType.Bus || entry.BusData == null) continue;

            Bus prefab = prefabSO.GetBus(entry.BusData.busType);
            if (prefab == null) continue;

            busCounts.TryGetValue(prefab, out int count);
            busCounts[prefab] = count + 1;
            ballCount += entry.BusData.GetBallCapacity();
        }

        foreach (KeyValuePair<Bus, int> pair in busCounts)
            ObjectPool.CreatePool(pair.Key, pair.Value);

        BallController ballPrefab = prefabSO.GetBall();
        if (ballPrefab != null)
            ObjectPool.CreatePool(ballPrefab, ballCount);
    }

    private int GetDefaultLevelIndex()
    {
        if (levelJson == null) return 1;

        string[] nameParts = levelJson.name.Split('_');
        return nameParts.Length > 1 && int.TryParse(nameParts[^1], out int index)
            ? Mathf.Max(1, index)
            : 1;
    }

    private bool TrySetLevelJson(int levelIndex)
    {
        TextAsset resourceLevel = Resources.Load<TextAsset>($"Level/Level_{levelIndex}");
        if (resourceLevel == null) return false;

        levelJson = resourceLevel;
        CurrentLevelIndex = levelIndex;
        return true;
    }

    private static bool HasLevel(int levelIndex)
    {
        return Resources.Load<TextAsset>($"Level/Level_{levelIndex}") != null;
    }
}

