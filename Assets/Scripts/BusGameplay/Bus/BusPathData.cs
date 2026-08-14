using System;
using UnityEngine;

[Serializable]
public class BusPathData
{
    public Vector3 startPosition;
    public int splineContainerIndex;
    public Vector3 nearestPoint;
    public float nearestT;

    public BusPathData Clone() => new()
    {
        startPosition = startPosition,
        splineContainerIndex = splineContainerIndex,
        nearestPoint = nearestPoint,
        nearestT = nearestT
    };
}
