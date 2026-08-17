using System;
using UnityEngine;

[Serializable]
public class LevelJsonData
{
    public int levelId;
    public float objectScale;
    public BusOutObjectEnrichedData[] busEnrichedDatas;
    public BusOutObjectEnrichedData[] garageEnrichedDatas;
    public TopGameConfigData topGameConfig;
}
