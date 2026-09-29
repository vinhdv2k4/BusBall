using System;
using System.Collections.Generic;
using UnityEngine;

public enum ColorId
{
    None = 0,
    Black = 1,
    Blue = 2,
    Brown = 3,
    Cyan = 4,
    Gray = 5,
    Green = 6,
    LightPink = 7,
    Lime = 8,
    Orange = 9,
    Periwinkle = 10,
    Pink = 11,
    Purple = 12,
    Red = 13,
    Teal = 14,
    Violet = 15,
    White = 16,
    Yellow = 17,
    LightBeige = 18,
    LightRed = 19,
    DarkGreen = 20,

    Wild = 99,
    Hidden = 100
}

[System.Serializable]
public class GameColorEntryData
{
    public ColorId id;
    public string displayName;
    public Color color;
    [Header("Materials")]
    public Material ballMaterial;
    public Material trayMaterial;
    public Material busMaterial;
}

[CreateAssetMenu(fileName = "GameColorConfig", menuName = "BusBallJam/Game Color Config")]
public class GameColorConfig : ScriptableObject
{
    [SerializeField]
    private List<GameColorEntryData> _colors = new()
    {
        new GameColorEntryData { id = ColorId.Black, displayName = "Black", color = HexToColor("#202020") },
        new GameColorEntryData { id = ColorId.Blue, displayName = "Blue", color = HexToColor("#3F7CFF") },
        new GameColorEntryData { id = ColorId.Brown, displayName = "Brown", color = HexToColor("#9A603F") },
        new GameColorEntryData { id = ColorId.Cyan, displayName = "Cyan", color = HexToColor("#4DE6FF") },
        new GameColorEntryData { id = ColorId.Gray, displayName = "Gray", color = HexToColor("#909090") },
        new GameColorEntryData { id = ColorId.Green, displayName = "Green", color = HexToColor("#4AD66D") },
        new GameColorEntryData { id = ColorId.LightPink, displayName = "LightPink", color = HexToColor("#FFB4D9") },
        new GameColorEntryData { id = ColorId.Lime, displayName = "Lime", color = HexToColor("#A9E64D") },
        new GameColorEntryData { id = ColorId.Orange, displayName = "Orange", color = HexToColor("#FF9A3F") },
        new GameColorEntryData { id = ColorId.Periwinkle, displayName = "Periwinkle", color = HexToColor("#829BFF") },
        new GameColorEntryData { id = ColorId.Pink, displayName = "Pink", color = HexToColor("#FF6FCF") },
        new GameColorEntryData { id = ColorId.Purple, displayName = "Purple", color = HexToColor("#9B5CFF") },
        new GameColorEntryData { id = ColorId.Red, displayName = "Red", color = HexToColor("#FF4A4A") },
        new GameColorEntryData { id = ColorId.Teal, displayName = "Teal", color = HexToColor("#32B8AC") },
        new GameColorEntryData { id = ColorId.Violet, displayName = "Violet", color = HexToColor("#714CFF") },
        new GameColorEntryData { id = ColorId.White, displayName = "White", color = Color.white },
        new GameColorEntryData { id = ColorId.Yellow, displayName = "Yellow", color = HexToColor("#FFD84A") },
        new GameColorEntryData { id = ColorId.LightBeige, displayName = "LightBeige", color = HexToColor("#F2D9B6") },
        new GameColorEntryData { id = ColorId.LightRed, displayName = "LightRed", color = HexToColor("#FF8A8A") },
        new GameColorEntryData { id = ColorId.DarkGreen, displayName = "DarkGreen", color = HexToColor("#1E7A3A") },
        new GameColorEntryData { id = ColorId.Wild, displayName = "Wild", color = Color.white }
    };

    public IReadOnlyList<GameColorEntryData> Colors => _colors;

    private static Color HexToColor(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var color);
        return color;
    }

    public GameColorEntryData Get(ColorId id)
    {
        for (var i = 0; i < _colors.Count; i++)
        {
            if (_colors[i].id == id) return _colors[i];
        }

        Debug.LogWarning($"Missing color config for {id}. Falling back to white.");
        return new GameColorEntryData
        {
            id = id,
            displayName = id.ToString(),
            color = Color.white
        };
    }

    public GameColorEntryData Get(ObjectColor objectColor)
    {
        switch (objectColor)
        {
            case ObjectColor.Blue:
                return Get(ColorId.Blue);
            case ObjectColor.Green:
                return Get(ColorId.Green);
            case ObjectColor.Red:
                return Get(ColorId.Red);
            case ObjectColor.Yellow:
                return Get(ColorId.Yellow);
            default:
                Debug.LogWarning($"GameColorConfig.Get: unsupported ObjectColor {objectColor}. Falling back to white.");
                return new GameColorEntryData
                {
                    id = ColorId.None,
                    displayName = objectColor.ToString(),
                    color = Color.white
                };
        }
    }

    public Color GetUnityColor(ColorId id)
    {
        return Get(id).color;
    }

    public Material GetBallMaterial(ColorId id)
    {
        return Get(id).ballMaterial;
    }

    public Material GetTrayMaterial(ColorId id)
    {
        return Get(id).trayMaterial;
    }

    public Material GetBusMaterial(ColorId id)
    {
        return Get(id).busMaterial;
    }

    public bool IsSameGameplayColor(ColorId a, ColorId b)
    {
        if (a == ColorId.None || b == ColorId.None) return false;
        if (a == ColorId.Wild || b == ColorId.Wild) return true;
        return a == b;
    }
}

public enum GameMaterialType
{
    Ball,
    Bus,
    Tray
}

public static class GameColorConfigProvider
{
    private const string ResourcePath = "GameColorConfig";
    private static GameColorConfig _instance;

    public static GameColorConfig Instance => _instance ??= Resources.Load<GameColorConfig>(ResourcePath);

    public static bool IsLoaded => Instance != null;

    public static GameColorEntryData Get(ColorId id)
    {
        if (Instance == null)
        {
            Debug.LogWarning($"GameColorConfigProvider: missing GameColorConfig at Resources/{ResourcePath}.asset");
            return new GameColorEntryData { id = id, displayName = id.ToString(), color = Color.white };
        }

        return Instance.Get(id);
    }

    public static Color GetUnityColor(ColorId id)
    {
        return Instance != null ? Instance.GetUnityColor(id) : Color.white;
    }

    public static Material GetMaterial(ColorId id, GameMaterialType type)
    {
        if (Instance == null)
        {
            Debug.LogWarning($"GameColorConfigProvider: missing GameColorConfig at Resources/{ResourcePath}.asset");
            return null;
        }

        return type switch
        {
            GameMaterialType.Ball => Instance.GetBallMaterial(id),
            GameMaterialType.Bus => Instance.GetBusMaterial(id),
            GameMaterialType.Tray => Instance.GetTrayMaterial(id),
            _ => null
        };
    }
}
