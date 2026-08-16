using UnityEngine;

public class DockSlot : MonoBehaviour
{
    private BallController ball;
    private DockSlotConveyor conveyor;

    public int SlotIndex { get; private set; } = -1;
    public bool HasBall => ball != null;
    public bool CanAcceptBall => ball == null;
    public BallController Ball => ball;

    public void Initialize(DockSlotConveyor owner, int index)
    {
        conveyor = owner;
        SlotIndex = index;
        ball = null;
    }

    public bool CanAccept(BallController candidate)
    {
        return candidate != null && (ball == null || ball == candidate);
    }

    public bool TryDockBall(BallController candidate, bool snap = false)
    {
        if (!CanAcceptBall || candidate == null) return false;
        ball = candidate;
        candidate.Dock(this, snap);
        conveyor?.OnBallDocked(SlotIndex);
        return true;
    }

    public BallController ReleaseBall(bool notifyConveyor = true)
    {
        BallController released = ball;
        ball = null;
        if (released != null) released.Undock();
        if (notifyConveyor) conveyor?.NotifyBallReleased(this);
        return released;
    }

    public BallController RemoveBallForConsumption()
    {
        BallController consumed = ball;
        ball = null;
        return consumed;
    }

    public void SetBall(BallController value)
    {
        ball = value;
        if (value != null) value.Dock(this, true);
    }
}
