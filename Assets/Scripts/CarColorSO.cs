using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Prototype/ProjectColorSO", fileName = "ProjectColorSO")]
public class ProjectColorSO : ScriptableObject
{
    public CarColorInfo[] carColors;

    public Color GetColor(ObjectColor color)
    {
        foreach (var entry in carColors)
        {
            if (entry.color == color)
                return entry.materialColor;
        }

        return Color.white;
    }
}

public static class ProjectColorConfig
{
    private const string ResourcePath = "ProjectColorSO";
    private static ProjectColorSO _instance;

    public static ProjectColorSO Instance => _instance ??= Resources.Load<ProjectColorSO>(ResourcePath);

    public static Color GetColor(ObjectColor color)
    {
        if (Instance == null)
        {
            Debug.LogWarning($"ProjectColorConfig: missing ProjectColorSO at Resources/{ResourcePath}.asset");
            return Color.white;
        }

        return Instance.GetColor(color);
    }
}

[Serializable]
public struct CarColorInfo
{
    public ObjectColor color;
    public Color materialColor;
}
