using System;
using UnityEngine;

[Serializable]
public class GridCoordinateData
{
    public float x;
    public float y;
}

[Serializable]
public class CarData
{
    public string carType;
    public ColorId eColor;
    public int size;
    public float angle;
    public float rotation;
    public Vector2 position;
    public GridCoordinateData gridPosition;

    public float CarAngle => angle != 0f ? angle : rotation;

    public bool HasGridPosition => gridPosition != null;

    public CarType type => size switch
    {
        0 => CarType.Small4Slots,
        1 => CarType.Medium6Slots,
        2 => CarType.Big9Slots,
        _ => CarType.Small4Slots
    };
}

[Serializable]
public class GuestData
{
    public ColorId eColor;
    public int number;
}

[Serializable]
public class LevelData
{
    public int eDifficulty;
    public CarData[] cars;
    public CarData[] carDatas;
    public GuestData[] guestDatas;

    public CarData[] GetCars()
    {
        return cars != null && cars.Length > 0 ? cars : carDatas ?? new CarData[0];
    }

    public GuestData[] GetGuests()
    {
        return guestDatas ?? new GuestData[0];
    }
}
