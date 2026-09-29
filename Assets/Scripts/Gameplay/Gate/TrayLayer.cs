using System;

namespace BallDropParty.Gameplay
{
    [Serializable]
    public sealed class TrayLayerData
    {
        public ColorId colorId;
        public int requiredCount = 3;
    }

    public sealed class TrayLayer
    {
        private int _reservedCount;

        public TrayLayer(ColorId colorId, int requiredCount, int initialCount = 0)
        {
            ColorId = colorId;
            RequiredCount = requiredCount;
            CurrentCount = ClampInitialCount(initialCount, RequiredCount);
        }

        public TrayLayer(TrayLayerData data, int initialCount = 0)
        {
            ColorId = data != null ? data.colorId : ColorId.None;
            RequiredCount = data != null ? data.requiredCount : 3;
            CurrentCount = ClampInitialCount(initialCount, RequiredCount);
        }

        public ColorId ColorId { get; }
        public int RequiredCount { get; }
        public int CurrentCount { get; private set; }
        public int ReservedCount => _reservedCount;
        public bool IsCompleted => CurrentCount >= RequiredCount;
        public bool HasOpenSlot => CurrentCount + _reservedCount < RequiredCount;

        public bool CanAccept(ColorId colorId, GameColorConfig colorConfig = null)
        {
            if (!HasOpenSlot) return false;
            if (colorConfig != null) return colorConfig.IsSameGameplayColor(colorId, ColorId);
            return ColorId == ColorId.None || colorId == ColorId || colorId == ColorId.Wild;
        }

        public int ReserveSlot()
        {
            if (!HasOpenSlot) return -1;
            var slotIndex = CurrentCount + _reservedCount;
            _reservedCount++;
            return slotIndex;
        }

        public void CommitReservedSlot()
        {
            if (_reservedCount > 0) _reservedCount--;
            CurrentCount++;
        }

        public void ReleaseReservedSlot()
        {
            if (_reservedCount > 0) _reservedCount--;
        }

        private static int ClampInitialCount(int count, int requiredCount)
        {
            if (count <= 0 || requiredCount <= 0) return 0;
            return count > requiredCount ? requiredCount : count;
        }
    }
}
