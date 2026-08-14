using System.Collections.Generic;
using UnityEngine;

public class FunnelController : MonoBehaviour
{
    [SerializeField] private Transform ballHolder;
    [SerializeField] private Transform busDropBallPoint;
    [SerializeField] private Transform funnelBot;
    private readonly Queue<BallController> balls = new();

    public int BallCount => balls.Count;
    public IReadOnlyCollection<BallController> Balls => balls;
    public Transform BallHolder => ballHolder;
    public Transform BusDropBallPoint => busDropBallPoint;
    public Transform FunnelBot => funnelBot;

    public void ReceiveBall(BallController ball)
    {
        if (ball != null && !balls.Contains(ball)) balls.Enqueue(ball);
    }

    public void ReceiveBalls(IEnumerable<BallController> receivedBalls)
    {
        if (receivedBalls == null) return;
        foreach (BallController ball in receivedBalls) ReceiveBall(ball);
    }

    public BallController TakeFirstBall()
    {
        return balls.Count == 0 ? null : balls.Dequeue();
    }
}
