using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineAnimate))]
public class Bus : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private BusData data = new();
    [SerializeField] private BusAnalysisData analysisData = new();
    [SerializeField] private BusPathData pathData = new();
    [Header("Renderers")]
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private Renderer topRenderer;
    [SerializeField] private Renderer glassRenderer;
    [SerializeField] private SpriteRenderer iconTop;
    [SerializeField] private DecoyBus decoyBus;
    [Header("Spline")]
    [SerializeField] private SplineAnimate splineAnimate;
    [Header("Animation")]
    [SerializeField] private BusAnimationController busAnimationController;
    [Header("Vfx Position")]
    [SerializeField] private Transform tfReleaseBall;
    [SerializeField] private Transform tfLanding;
    [SerializeField] private Transform tfSmoke;
    [SerializeField] private Transform tfHurrySmoke;
    [Header("Ball Transforms")]
    [SerializeField] private List<Transform> ballTransforms = new();
    [Header("Hurry Bus Light")]
    [SerializeField] private GameObject[] hurryBusLight;
    [SerializeField] private Transform dropParent;
    [SerializeField] private FunnelController funnel;
    [Header("Di chuyển cơ bản - sẽ nối spline sau")]
    private readonly List<BallController> balls = new();
    private Collider busCollider;
    private bool isBlocked;

    public IReadOnlyList<BallController> Balls => balls;
    public int BallCount => balls.Count;
    public BusState State { get; private set; } = BusState.Idle;
    public bool IsBlocked => isBlocked;
    public event Action<bool> BlockedChanged;
    public event Action<BusState> StateChanged;
    public event Action<HitDirection> Hit;

    public BusData Data => data;
    public BusAnalysisData AnalysisData => analysisData;
    public BusPathData PathData => pathData;
    public IReadOnlyList<Transform> BallTransforms => ballTransforms;

    private void Awake()
    {
        busCollider = GetComponent<Collider>();
        foreach (Transform slot in ballTransforms)
        {
            BallController ball = slot != null ? slot.GetComponentInChildren<BallController>() : null;
            if (ball != null && !balls.Contains(ball)) balls.Add(ball);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider == null || collision.collider.transform.IsChildOf(transform)) return;
        HitDirection direction = BusUtils.GetHitDirection(transform, collision.collider);
        ChangeState(BusState.GotHit);
        busAnimationController?.PlayHit(direction);
        Hit?.Invoke(direction);
        SetBlocked(true);
    }

    public void StartMoveStraight()
    {
        ChangeState(BusState.MoveStraight);
    }

    public void Configure(BusData data, Vector3 position, float yRotation)
    {
        this.data = data?.Clone() ?? new BusData();
        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yRotation, 0f));
    }

    public void Stop()
    {
        ChangeState(BusState.Finished);
    }

    private void SetBlocked(bool value)
    {
        if (isBlocked == value) return;
        isBlocked = value;
        ChangeState(value ? BusState.Blocked : BusState.MoveStraight);
        BlockedChanged?.Invoke(value);
    }

    private void ChangeState(BusState nextState)
    {
        if (State == nextState) return;
        State = nextState;
        StateChanged?.Invoke(nextState);
    }

    public List<BallController> DropBall()
    {
        List<BallController> dropped = new(balls);
        balls.Clear();
        foreach (BallController ball in dropped)
        {
            if (ball == null) continue;
            if (dropParent != null) ball.transform.SetParent(dropParent, true);
            funnel?.ReceiveBall(ball);
        }
        return dropped;
    }

    public bool AddBall(BallController ball)
    {
        if (ball == null || balls.Contains(ball)) return false;
        balls.Add(ball);
        ball.transform.SetParent(transform, true);
        return true;
    }
}
