using System;
using System.Collections.Generic;

[Serializable]
public class BusData
{
    public BusType busType;
    public ColorType colorType;
    public BusMechanicType mechanicType;
    public List<BusMechanicConfig> MechanicData = new();

    public static int GetBallCapacity(BusType type)
    {
        return type switch
        {
            BusType.Small => 6,
            BusType.Medium => 9,
            BusType.Large => 12,
            _ => 6
        };
    }

    public int GetBallCapacity() => GetBallCapacity(busType);

    public bool HasMechanic(BusMechanicType type)
    {
        if (type == mechanicType) return true;
        return MechanicData != null && MechanicData.Exists(item => item != null && item.MechanicType == type);
    }

    public bool IsFrozenBusMechanic() => HasMechanic(BusMechanicType.FrozenBus);
    public bool IsHurryBusMechanic() => HasMechanic(BusMechanicType.HurryBus);
    public bool IsHiddenBusMechanic() => HasMechanic(BusMechanicType.HiddenBus);

    public BusData Clone()
    {
        BusData copy = new() { busType = busType, colorType = colorType, mechanicType = mechanicType };
        if (MechanicData != null)
            foreach (BusMechanicConfig mechanic in MechanicData)
                if (mechanic != null) copy.MechanicData.Add(new BusMechanicConfig { MechanicType = mechanic.MechanicType, CustomData = mechanic.CustomData });
        return copy;
    }
}
