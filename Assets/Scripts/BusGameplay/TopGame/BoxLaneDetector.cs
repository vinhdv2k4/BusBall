using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BoxLaneDetector : MonoBehaviour
{
    [SerializeField] private float processCooldown = 0.02f;
    [SerializeField, Range(0f, 1f)] private float requiredSplineProgress = 0.85f;
    private readonly Queue<Collider2D> pending = new();
    private readonly HashSet<Collider2D> enqueued = new();
    private readonly List<Collider2D> overlapResults = new();
    private Collider2D detectorCollider;
    private BoxLane lane;
    private Coroutine routine;
    public void Initialize(BoxLane owner) => lane = owner;
    private void Awake()
    {
        detectorCollider = GetComponent<Collider2D>();
        detectorCollider.isTrigger = true;
        lane ??= GetComponentInParent<BoxLane>();
        Debug.Log($"BoxLaneDetector {name}: ready. Lane={(lane != null ? lane.name : "null")}, Collider={detectorCollider != null}.", this);
    }

    private void FixedUpdate()
    {
        if (detectorCollider == null) return;

        overlapResults.Clear();
        ContactFilter2D filter = ContactFilter2D.noFilter;
        filter.useTriggers = true;
        detectorCollider.Overlap(filter, overlapResults);
        foreach (Collider2D other in overlapResults)
            Enqueue(other);
    }

    public bool HasReachedSplineProgress(BallController ball)
    {
        DockSlotConveyor conveyor = lane != null
            ? FindFirstObjectByType<DockSlotConveyor>()
            : null;
        bool reached = conveyor != null && conveyor.HasReachedProgress(ball, requiredSplineProgress);
        Debug.Log($"BoxLaneDetector {name}: spline progress reached={reached}.", this);
        return reached;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        Enqueue(other);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Enqueue(other);
    }

    private void Enqueue(Collider2D other)
    {
        if (other == null || other.GetComponentInParent<BallController>() == null || !enqueued.Add(other)) return;
        BallController ball = other.GetComponentInParent<BallController>();
        Debug.Log($"BoxLaneDetector {name}: detected ball {ball.name}.", this);
        pending.Enqueue(other); routine ??= StartCoroutine(ProcessQueueRoutine());
    }
    private IEnumerator ProcessQueueRoutine()
    {
        while (pending.Count > 0)
        {
            Collider2D other = pending.Dequeue();
            enqueued.Remove(other);
            BallController ball = other != null ? other.GetComponentInParent<BallController>() : null;
            Debug.Log($"BoxLaneDetector {name}: processing ball {(ball != null ? ball.name : "null")}.", this);
            lane?.ProcessBallInDetector(other);
            yield return new WaitForSeconds(processCooldown);
        }
        routine = null;
    }
}
