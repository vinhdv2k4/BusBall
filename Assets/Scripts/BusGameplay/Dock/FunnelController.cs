using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FunnelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform ballHolder;
    [SerializeField] private Transform busDropBallPoint;
    [SerializeField] private Transform funnelBot;
    [SerializeField] private DockSlotConveyor dockSlotConveyor;
    [Tooltip("Collider2D covering the gate/receiver area. Only balls physically overlapping this area may dock.")]
    [SerializeField] private Collider2D dockReceiveArea;
    [Tooltip("Radius around Funnel Bot where a ball is allowed to enter the next slot.")]
    [SerializeField, Min(0.01f)] private float funnelBotOverlapRadius = 0.5f;
    [SerializeField] private LayerMask ballLayer = ~0;

    [Header("Unstuck")]
    [SerializeField, Min(0f)] private float stuckVelocityThreshold = 0.01f;
    [SerializeField, Min(0f)] private float unstuckHorizontalForce = 0.05f;

    private readonly List<BallController> balls = new();
    private readonly List<BallController> overlapCandidates = new();
    private readonly List<BallController> unstuckCandidates = new();
    private readonly List<Collider2D> overlapResults = new();
    private Coroutine unstuckRoutine;

    public int BallCount => balls.Count;
    public IReadOnlyCollection<BallController> Balls => balls;
    public Transform BallHolder => ballHolder;
    public Transform BusDropBallPoint => busDropBallPoint;
    public Transform FunnelBot => funnelBot;

    private void Awake()
    {
        if (dockSlotConveyor == null)
            dockSlotConveyor = FindFirstObjectByType<DockSlotConveyor>();
        if (dockReceiveArea == null && funnelBot != null)
            dockReceiveArea = funnelBot.GetComponent<Collider2D>();
        if (dockReceiveArea == null)
            dockReceiveArea = GetComponent<Collider2D>();
    }

    private void OnDrawGizmosSelected()
    {
        if (funnelBot == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(funnelBot.position, funnelBotOverlapRadius);
    }

    private void FixedUpdate()
    {
        List<DockSlot> availableSlots = dockSlotConveyor != null
            ? dockSlotConveyor.GetAvailableManagedSlots()
            : null;
        if (availableSlots == null || availableSlots.Count == 0) return;

        CollectOverlapCandidates(availableSlots);
        DockOverlapCandidates(availableSlots);
        StartUnstuckRoutineIfNeeded();
    }

    public void ReceiveBall(BallController ball)
    {
        if (ball != null && !balls.Contains(ball)) balls.Add(ball);
    }

    public void ReceiveBalls(IEnumerable<BallController> receivedBalls)
    {
        if (receivedBalls == null) return;
        foreach (BallController ball in receivedBalls) ReceiveBall(ball);
    }

    // Kept only for legacy callers. Funnel docking itself never consumes FIFO.
    public BallController TakeFirstBall()
    {
        if (balls.Count == 0) return null;
        BallController ball = balls[0];
        balls.RemoveAt(0);
        return ball;
    }

    private void CollectOverlapCandidates(IReadOnlyList<DockSlot> availableSlots)
    {
        overlapCandidates.Clear();
        overlapResults.Clear();
        if (dockReceiveArea == null) return;

        ContactFilter2D filter = new()
        {
            useLayerMask = true,
            layerMask = ballLayer,
            useTriggers = true
        };
        dockReceiveArea.Overlap(filter, overlapResults);

        foreach (Collider2D overlap in overlapResults)
        {
            BallController ball = overlap != null ? overlap.GetComponentInParent<BallController>() : null;
            if (!IsValidOverlapCandidate(ball)) continue;
            if (!overlapCandidates.Contains(ball)) overlapCandidates.Add(ball);
        }

        // Overlap decides eligibility. Distance only decides priority.
        overlapCandidates.Sort((left, right) => CompareCandidates(left, right, availableSlots));
    }

    private bool IsValidOverlapCandidate(BallController ball)
    {
        if (ball == null || !balls.Contains(ball)) return false;
        if (ball.IsDocked || ball.IsCompacting || ball.IsMoving) return false;
        if (ball.CirCollider == null || !ball.CirCollider.enabled) return false;
        if (ball.Rb == null || !ball.Rb.simulated) return false;
        if ((ballLayer.value & (1 << ball.gameObject.layer)) == 0) return false;

        // Being inside the broad receive area is not enough. The ball must also
        // be close to the FunnelBot before it can enter the next free slot.
        if (funnelBot == null) return false;
        float radius = funnelBotOverlapRadius + ball.CirCollider.radius * ball.transform.lossyScale.x;
        return (ball.transform.position - funnelBot.position).sqrMagnitude <= radius * radius;
    }

    private int CompareCandidates(BallController left, BallController right, IReadOnlyList<DockSlot> slots)
    {
        float leftDistance = GetClosestSlotDistanceSquared(left, slots);
        float rightDistance = GetClosestSlotDistanceSquared(right, slots);
        return leftDistance.CompareTo(rightDistance);
    }

    private void DockOverlapCandidates(List<DockSlot> availableSlots)
    {
        // Only the slot nearest to the funnel bot is eligible. This keeps balls
        // entering the conveyor in order instead of filling arbitrary slots.
        DockSlot funnelSlot = GetClosestAvailableSlot(
            funnelBot,
            availableSlots,
            funnelBotOverlapRadius);
        if (funnelSlot == null) return;

        while (overlapCandidates.Count > 0)
        {
            BallController ball = overlapCandidates[0];
            overlapCandidates.RemoveAt(0);
            if (!IsValidOverlapCandidate(ball)) continue;

            if (!funnelSlot.TryDockBall(ball)) continue;

            balls.Remove(ball);
            availableSlots.Remove(funnelSlot);
            break;
        }
    }

    private static DockSlot GetClosestAvailableSlot(
        Transform origin,
        IReadOnlyList<DockSlot> slots,
        float maxDistance = float.PositiveInfinity)
    {
        if (origin == null) return null;

        DockSlot closest = null;
        float closestDistance = float.MaxValue;
        float maxDistanceSquared = maxDistance * maxDistance;
        foreach (DockSlot slot in slots)
        {
            if (slot == null || !slot.CanAcceptBall) continue;
            float distance = (slot.transform.position - origin.position).sqrMagnitude;
            if (distance > maxDistanceSquared) continue;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = slot;
            }
        }
        return closest;
    }

    private static DockSlot GetClosestAvailableSlot(BallController ball, IReadOnlyList<DockSlot> slots)
    {
        DockSlot closest = null;
        float closestDistance = float.MaxValue;
        foreach (DockSlot slot in slots)
        {
            if (slot == null || !slot.CanAcceptBall) continue;
            float distance = (slot.transform.position - ball.transform.position).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = slot;
            }
        }
        return closest;
    }

    private static float GetClosestSlotDistanceSquared(BallController ball, IReadOnlyList<DockSlot> slots)
    {
        DockSlot slot = GetClosestAvailableSlot(ball, slots);
        return slot == null ? float.MaxValue : (slot.transform.position - ball.transform.position).sqrMagnitude;
    }

    private void StartUnstuckRoutineIfNeeded()
    {
        foreach (BallController ball in balls)
        {
            if (ball == null || ball.IsDocked || ball.IsMoving || ball.Rb == null || !ball.Rb.simulated) continue;
            if (ball.Rb.linearVelocity.sqrMagnitude <= stuckVelocityThreshold * stuckVelocityThreshold &&
                !unstuckCandidates.Contains(ball))
                unstuckCandidates.Add(ball);
        }

        if (unstuckRoutine == null && unstuckCandidates.Count > 0)
            unstuckRoutine = StartCoroutine(UnstuckBallsRoutine());
    }

    private IEnumerator UnstuckBallsRoutine()
    {
        yield return new WaitForSeconds(0.1f);
        foreach (BallController ball in unstuckCandidates)
        {
            if (ball == null || ball.IsDocked || ball.IsMoving) continue;
            ball.AddRandomHorizontalForce(-unstuckHorizontalForce, unstuckHorizontalForce);
        }
        unstuckCandidates.Clear();
        unstuckRoutine = null;
    }
}
