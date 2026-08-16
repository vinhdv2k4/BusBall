using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BoxLaneDetector : MonoBehaviour
{
    [SerializeField] private float processCooldown = 0.02f;
    [Header("Collider to Detect")]
    [SerializeField] private Collider2D detectedCollider;
    private readonly Queue<Collider2D> pending = new();
    private readonly HashSet<Collider2D> enqueued = new();
    private Collider2D detectorCollider;
    private BoxLane lane;
    private Coroutine routine;
    public void Initialize(BoxLane owner) => lane = owner;

    public bool IsDetecting(BallController ball)
    {
        return detectorCollider != null && ball != null && ball.CirCollider != null &&
            detectorCollider.IsTouching(ball.CirCollider);
    }
    private void Awake()
    {
        detectorCollider = detectedCollider;
        if (detectorCollider == null)
        {
            Debug.LogError(
                $"BoxLaneDetector {name}: Detected Collider chưa được gán.",
                this);
            enabled = false;
            return;
        }

        detectorCollider.isTrigger = true;
        lane ??= GetComponentInParent<BoxLane>();
        Debug.Log($"BoxLaneDetector {name}: ready. Lane={(lane != null ? lane.name : "null")}, Collider={detectorCollider != null}.", this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"BoxLaneDetector {name}: OnTriggerEnter2D detected {other.name}.", this);
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
