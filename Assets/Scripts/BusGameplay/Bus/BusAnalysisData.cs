using System;

[Serializable]
public class BusAnalysisData
{
    public int blockedBusCount;
    public int depth;
    // Stored by the level exporter as packed little-endian 32-bit indices.
    public string busesBlockedByThis;
    public bool IsBlocked() => blockedBusCount > 0;

    public int[] GetBlockedBusIndices()
    {
        if (string.IsNullOrEmpty(busesBlockedByThis)) return Array.Empty<int>();

        int count = busesBlockedByThis.Length / 8;
        int[] result = new int[count];
        for (int i = 0; i < count; i++)
        {
            string packed = busesBlockedByThis.Substring(i * 8, 8);
            byte[] bytes = new byte[4];
            for (int j = 0; j < 4; j++)
                bytes[j] = Convert.ToByte(packed.Substring(j * 2, 2), 16);
            result[i] = BitConverter.ToInt32(bytes, 0);
        }
        return result;
    }

    public BusAnalysisData Clone()
    {
        return new BusAnalysisData
        {
            blockedBusCount = blockedBusCount,
            depth = depth,
            busesBlockedByThis = busesBlockedByThis
        };
    }
}
