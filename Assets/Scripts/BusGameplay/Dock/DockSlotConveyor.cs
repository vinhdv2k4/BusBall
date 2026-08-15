using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

public class DockSlotConveyor : MonoBehaviour
{
    [SerializeField] private List<DockSlot> dockSlots = new();
    [SerializeField] private float compactDuration = 0.15f;
    [SerializeField, Range(0f, 1f)] private float debugSplineProgress = 0.85f;
    [SerializeField, Min(0.001f)] private float debugProgressTolerance = 0.03f;

    private int _leaderSlotIndex;

    public IReadOnlyList<DockSlot> Slots => dockSlots;
    public int Count => dockSlots.Count;
    public int LeaderSlotIndex => _leaderSlotIndex;

    public bool HasReachedProgress(BallController ball, float requiredProgress)
    {
        if (ball == null || ball.Slot == null) return false;
        SplineAnimate spline = ball.Slot.GetComponent<SplineAnimate>();
        if (spline == null) return false;
        return spline.NormalizedTime >= Mathf.Clamp01(requiredProgress);
    }

    private void OnDrawGizmosSelected()
    {
        foreach (DockSlot slot in GetComponentsInChildren<DockSlot>(true))
        {
            if (slot == null) continue;
            SplineAnimate spline = slot.GetComponent<SplineAnimate>();
            if (spline == null) continue;

            float distance = Mathf.Abs(spline.NormalizedTime - debugSplineProgress);
            Gizmos.color = distance <= debugProgressTolerance ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(slot.transform.position, 0.08f);
        }
    }

    private void Awake()
    {
        dockSlots = GetComponentsInChildren<DockSlot>(true)
            .OrderBy(slot => slot.transform.GetSiblingIndex())
            .ToList();

        _leaderSlotIndex = 0;

        for (int i = 0; i < dockSlots.Count; i++)
            if (dockSlots[i] != null) dockSlots[i].Initialize(this, i);
    }

    public List<DockSlot> GetAvailableManagedSlots()
    {
        List<DockSlot> result = new();
        foreach (DockSlot slot in dockSlots)
            if (slot != null && slot.CanAcceptBall) result.Add(slot);
        return result;
    }

    public bool TryDock(BallController ball)
    {
        if (ball == null) return false;
        DockSlot slot = GetAvailableManagedSlots().Count > 0 ? GetAvailableManagedSlots()[0] : null;
        return slot != null && slot.TryDockBall(ball);
    }

    public List<BallController> GetBallsByColor(ColorType color, int maxCount = 9)
    {
        List<BallController> result = new();
        foreach (DockSlot slot in dockSlots)
        {
            if (slot?.Ball != null && slot.Ball.Color == color)
            {
                result.Add(slot.Ball);
                if (result.Count >= maxCount) break;
            }
        }
        return result;
    }

    public void UndockBalls(IReadOnlyList<BallController> balls)
    {
        if (balls == null) return;
        foreach (BallController candidate in balls)
        {
            if (candidate?.Slot == null) continue;
            candidate.Slot.ReleaseBall();
        }
        Compact();
    }

    public void NotifyBallDocked(DockSlot slot) { }

    public void OnBallDocked(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= dockSlots.Count) return;
        NotifyBallDocked(dockSlots[slotIndex]);
    }

    public void NotifyBallReleased(DockSlot slot)
    {
        if (slot != null)
            OnBallReleasedFromConveyor(slot.SlotIndex);
    }

    public void OnBallReleasedFromConveyor(int slotIndex)
    {
        if (dockSlots.Count == 0) return;
        AdvanceLeaderFrom(slotIndex);
        TryCompactFollowers();
    }

    private void AdvanceLeaderFrom(int releasedIndex)
    {
        // The leader is the first logical position in the conveyor queue.
        // When a follower is released, the leader itself does not change;
        // compacting fills the released position from behind.
        if (_leaderSlotIndex < 0 || _leaderSlotIndex >= dockSlots.Count)
            _leaderSlotIndex = 0;
    }

    private void TryCompactFollowers()
    {
        if (dockSlots.Count <= 1) return;
        FillFollowersInForwardOrderFromLeader(dockSlots.Count);
    }

    private void FillFollowersInForwardOrderFromLeader(int slotCount)
    {
        int targetIndex = _leaderSlotIndex;

        for (int step = 0; step < slotCount; step++)
        {
            int sourceIndex = GetNextSlotIndex(_leaderSlotIndex, step, slotCount);
            DockSlot sourceSlot = dockSlots[sourceIndex];
            if (sourceSlot?.Ball == null) continue;

            if (sourceIndex != targetIndex)
                MoveFollowerOneStep(sourceIndex, targetIndex);

            targetIndex = GetNextSlotIndex(targetIndex, 1, slotCount);
        }
    }

    private void MoveFollowerOneStep(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return;

        DockSlot sourceSlot = dockSlots[fromIndex];
        DockSlot targetSlot = dockSlots[toIndex];
        if (sourceSlot?.Ball == null || targetSlot == null || targetSlot.HasBall) return;

        BallController follower = sourceSlot.ReleaseBall(false);
        targetSlot.SetBall(follower);
        follower.MoveToConveyorSlotLinear(targetSlot, compactDuration);
    }

    private static int GetNextSlotIndex(int startIndex, int offset, int slotCount)
    {
        return (startIndex + offset % slotCount + slotCount) % slotCount;
    }

    public void Compact()
    {
        TryCompactFollowers();
    }
}
