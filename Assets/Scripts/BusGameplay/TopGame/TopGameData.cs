using System;
using System.Collections.Generic;

[Serializable]
public class TopGameConfigData
{
    public List<BoxLaneConfigData> boxLaneConfigs = new();
}

[Serializable]
public class BoxLaneConfigData
{
    public List<BoxDataConfig> boxDataConfigs = new();
}

[Serializable]
public class BoxDataConfig
{
    public ColorType colorType;
    public List<BusMechanicConfig> mechanicData = new();
}
