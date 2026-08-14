using System;
using UnityEngine;

[Serializable]
public class BusOutObjectEnrichedData
{
    public BusOutObjectType Type;
    public Vector3 Position;
    public int Rotation;
    public BusData BusData;
    public GarageData GarageData;
    public BusAnalysisData AnalysisData = new();
    public BusPathData BusPathData = new();

    public BusOutObjectEnrichedData Clone() => new()
    {
        Type = Type, Position = Position, Rotation = Rotation,
        BusData = BusData?.Clone(), GarageData = GarageData?.Clone(),
        AnalysisData = AnalysisData?.Clone(), BusPathData = BusPathData?.Clone()
    };
}
