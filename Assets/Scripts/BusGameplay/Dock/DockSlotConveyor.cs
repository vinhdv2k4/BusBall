using System.Collections.Generic;
using UnityEngine;

public class DockSlotConveyor : MonoBehaviour
{
    [SerializeField] private List<DockSlot> dockSlots = new();
    [SerializeField] private float compactDuration = 0.15f;

    public IReadOnlyList<DockSlot> Slots => dockSlots;
    public int Count => dockSlots.Count;

    private void Awake()
    {
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

    public void NotifyBallReleased(DockSlot slot)
    {
        Compact();
    }

    public void Compact()
    {
        int target = 0;
        for (int source = 0; source < dockSlots.Count; source++)
        {
            DockSlot sourceSlot = dockSlots[source];
            if (sourceSlot?.Ball == null) continue;
            if (target != source)
            {
                BallController movingBall = sourceSlot.ReleaseBall(false);
                dockSlots[target].SetBall(movingBall);
                movingBall.MoveTo(dockSlots[target].transform.position, compactDuration);
            }
            target++;
        }
    }
}
