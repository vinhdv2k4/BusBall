using System;

[Serializable]
public class BusAnalysisData
{
    public int blockedBusCount;
    public int depth;
    public int[] busesBlockedByThis;
    public bool IsBlocked() => blockedBusCount > 0;

    public BusAnalysisData Clone()
    {
        return new BusAnalysisData
        {
            blockedBusCount = blockedBusCount,
            depth = depth,
            busesBlockedByThis = busesBlockedByThis == null ? null : (int[])busesBlockedByThis.Clone()
        };
    }
}
