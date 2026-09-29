using UnityEngine;

public static class DataManager
{
    public static LevelData LoadLevel(string levelName)
    {
        var primaryPath = $"Levels/{levelName}";
        var json = Resources.Load<TextAsset>(primaryPath);

        if (json == null)
        {
            string alternatePath = null;
            if (int.TryParse(levelName, out _))
            {
                alternatePath = $"Levels/level{levelName}";
            }
            else if (levelName.StartsWith("level"))
            {
                alternatePath = $"Levels/{levelName.Substring(5)}";
            }

            if (!string.IsNullOrEmpty(alternatePath))
            {
                json = Resources.Load<TextAsset>(alternatePath);
                if (json != null)
                {
                    primaryPath = alternatePath;
                }
            }
        }

        if (json == null)
        {
            Debug.LogWarning($"DataManager.LoadLevel: Level JSON not found at Resources/{primaryPath}.json");
            return new LevelData { cars = new CarData[0] };
        }

        var levelData = JsonUtility.FromJson<LevelData>(json.text);
        if (levelData == null)
        {
            Debug.LogWarning($"DataManager.LoadLevel: JSON parse failed for Resources/{primaryPath}.json\n{json.text}");
            return new LevelData { cars = new CarData[0] };
        }

        levelData.cars = levelData.GetCars();
        if (levelData.cars == null || levelData.cars.Length == 0)
        {
            Debug.LogWarning($"DataManager.LoadLevel: No cars found in Resources/{primaryPath}.json");
            levelData.cars = new CarData[0];
        }

        return levelData;
    }

    public static LevelData LoadLevel(int levelIndex)
    {
        return LoadLevel(levelIndex.ToString());
    }
}
