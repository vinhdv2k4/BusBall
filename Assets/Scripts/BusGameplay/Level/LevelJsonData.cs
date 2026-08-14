using System;
using UnityEngine;

[Serializable]
public class LevelJsonData
{
    public int levelId;
    public BusOutObjectEnrichedData[] busEnrichedDatas;
    public BusOutObjectEnrichedData[] garageEnrichedDatas;
    public TopGameConfigData topGameConfig;
}
