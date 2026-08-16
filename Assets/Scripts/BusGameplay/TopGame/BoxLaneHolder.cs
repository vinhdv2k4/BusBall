using System.Collections.Generic;
using UnityEngine;

public class BoxLaneHolder : MonoBehaviour
{
    [HideInInspector] private readonly List<BoxLane> boxLanes = new();
    [SerializeField] private PrefabSO prefabSO;
    [Header("Box Lane Spawn Points")]
    [SerializeField] private Transform gate0SpawnPoint;
    [SerializeField] private Transform gate1SpawnPoint;
    [SerializeField] private Transform gate2SpawnPoint;
    [SerializeField] private Transform gate3SpawnPoint;
    public List<BoxLane> LstBoxLanes => boxLanes;

    public void Init(TopGameConfigData topLevelConfig, PrefabSO prefabDatabase = null)
    {
        if (prefabDatabase != null) prefabSO = prefabDatabase;
        ResetData();
        if (topLevelConfig?.boxLaneConfigs == null) return;
        for (int i = 0; i < topLevelConfig.boxLaneConfigs.Count; i++)
        {
            BoxLaneConfigData config = topLevelConfig.boxLaneConfigs[i];
            BoxLane lane = prefabSO?.GetBoxLanePrefab() != null
                ? Instantiate(prefabSO.GetBoxLanePrefab(), transform)
                : new GameObject("BoxLane").AddComponent<BoxLane>();
            lane.name = $"BoxLane_Gate_{i}";
            lane.transform.SetParent(transform, false);
            Transform spawnPoint = GetSpawnPoint(i);
            if (spawnPoint != null)
                lane.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            lane.Init(config, prefabSO != null ? prefabSO.GetBoxPrefab()?.gameObject : null);
            boxLanes.Add(lane);
        }
    }

    private Transform GetSpawnPoint(int gateIndex)
    {
        return gateIndex switch
        {
            0 => gate0SpawnPoint,
            1 => gate1SpawnPoint,
            2 => gate2SpawnPoint,
            3 => gate3SpawnPoint,
            _ => null
        };
    }

    public void ResetData()
    {
        foreach (BoxLane lane in boxLanes)
            if (lane != null) Destroy(lane.gameObject);
        boxLanes.Clear();
    }

    public void DestroyAndMoveBoxUp(Box box)
    {
        if (box == null) return;
        foreach (BoxLane lane in boxLanes)
            if (lane.RemoveBox(box)) break;
    }

    public List<ColorType> GetAllFirstBoxColors()
    {
        List<ColorType> colors = new();
        foreach (BoxLane lane in boxLanes)
            if (lane.TryGetFirstColor(out ColorType color)) colors.Add(color);
        return colors;
    }

    public List<ColorType> GetAllLevelBoxColors()
    {
        List<ColorType> colors = new();
        foreach (BoxLane lane in boxLanes) colors.AddRange(lane.GetAllColors());
        return colors;
    }

    public int GetTotalBoxLeft()
    {
        int count = 0;
        foreach (BoxLane lane in boxLanes) count += lane.Count;
        return count;
    }

    public int GetTotalBoxConfig()
    {
        int count = 0;
        foreach (BoxLane lane in boxLanes) count += lane.ConfigCount;
        return count;
    }

    public bool TryConsumeBall(ColorType color)
    {
        foreach (BoxLane lane in boxLanes)
            if (lane.RemoveFirstBox(color)) return true;
        return false;
    }

    public float GetTotalBoxLeftRatio()
    {
        int total = GetTotalBoxConfig();
        return total == 0 ? 0f : (float)GetTotalBoxLeft() / total;
    }
}
