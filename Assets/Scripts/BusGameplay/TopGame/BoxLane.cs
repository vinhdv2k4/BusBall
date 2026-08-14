using System.Collections.Generic;
using UnityEngine;

public class BoxLane : MonoBehaviour
{
    private readonly List<BoxDataConfig> boxes = new();
    public int Count => boxes.Count;
    public int ConfigCount { get; private set; }

    public void Init(BoxLaneConfigData config)
    {
        boxes.Clear();
        if (config?.boxDataConfigs == null) return;
        boxes.AddRange(config.boxDataConfigs);
        ConfigCount = boxes.Count;
    }

    public bool TryGetFirstColor(out ColorType color)
    {
        if (boxes.Count == 0 || boxes[0] == null)
        {
            color = ColorType.None;
            return false;
        }
        color = boxes[0].colorType;
        return true;
    }

    public List<ColorType> GetAllColors()
    {
        List<ColorType> colors = new();
        foreach (BoxDataConfig box in boxes)
            if (box != null) colors.Add(box.colorType);
        return colors;
    }

    public bool RemoveFirstBox(ColorType color)
    {
        if (!TryGetFirstColor(out ColorType firstColor) || firstColor != color) return false;
        boxes.RemoveAt(0);
        return true;
    }

    public bool RemoveBox(Box box)
    {
        if (boxes.Count == 0) return false;
        boxes.RemoveAt(0);
        return true;
    }
}
