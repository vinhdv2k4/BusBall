using System;
using System.Collections.Generic;

[Serializable]
public class GarageData
{
    public List<BusData> Buses = new();

    public GarageData Clone()
    {
        GarageData copy = new();
        if (Buses != null)
            foreach (BusData bus in Buses)
                if (bus != null) copy.Buses.Add(bus.Clone());
        return copy;
    }
}
