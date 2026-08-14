using System.Collections.Generic;
using UnityEngine;

public class BoxLaneHolder : MonoBehaviour
{
    [SerializeField] private List<BoxLane> boxLanes = new();
    public List<BoxLane> LstBoxLanes => boxLanes;

    public void Init(TopGameConfigData topLevelConfig)
    {
        ResetData();
        if (topLevelConfig?.boxLaneConfigs == null) return;
        foreach (BoxLaneConfigData config in topLevelConfig.boxLaneConfigs)
        {
            BoxLane lane = new GameObject("BoxLane").AddComponent<BoxLane>();
            lane.transform.SetParent(transform, false);
            lane.Init(config);
            boxLanes.Add(lane);
        }
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
