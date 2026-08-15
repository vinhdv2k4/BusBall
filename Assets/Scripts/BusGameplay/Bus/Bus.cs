using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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
    [SerializeField] private RoadPath roadPath;
    [Header("Animation")]
    [SerializeField] private BusAnimationController busAnimationController;
    [Header("Vfx Position")]
    [SerializeField] private Transform tfReleaseBall;
    [SerializeField] private Transform tfLanding;
    [SerializeField] private Transform tfSmoke;
    [SerializeField] private Transform tfHurrySmoke;
    [Header("Ball Transforms")]
    [SerializeField] private List<Transform> ballTransforms = new();
    private BallController ballPrefab;
    [Header("Hurry Bus Light")]
    [SerializeField] private GameObject[] hurryBusLight;
    [SerializeField] private Transform dropParent;
    [SerializeField] private FunnelController funnel;
    [SerializeField] private TopGameController topGameController;
    [Header("Movement")]
    [SerializeField] private BusManager busManager;
    [SerializeField] private LayerMask blockMask = ~0;
    [SerializeField] private bool enableClickInput = true;
    [Header("Di chuyển cơ bản - sẽ nối spline sau")]
    private readonly List<BallController> balls = new();
    private Collider busCollider;
    private bool isBlocked;
    private Coroutine movementRoutine;
    private readonly List<Bus> blockingBuses = new();
    private readonly List<Bus> busesBlockedByThis = new();
    private int levelIndex = -1;
    private bool releasedBlockedBuses;
    private bool hasFinishedRoad;
    private bool hasOpenedOnRoad;
    private bool isDespawning;
    private int lastReachedKnotIndex = -1;
    private float activeKnotZRotation;
    private const float ExitDistance = 10f;
    private const float ExitSpeed = 10f;

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
    public int LevelIndex => levelIndex;

    private void Awake()
    {
        if (busManager == null)
            busManager = FindFirstObjectByType<BusManager>();
        if (roadPath == null)
            roadPath = FindFirstObjectByType<RoadPath>();
        if (funnel == null)
            funnel = FindFirstObjectByType<FunnelController>();
        if (topGameController == null)
            topGameController = FindFirstObjectByType<TopGameController>();

        busCollider = GetComponent<Collider>();
        if (splineAnimate != null)
            splineAnimate.Completed += OnSplineCompleted;
        foreach (Transform slot in ballTransforms)
        {
            BallController ball = slot != null ? slot.GetComponentInChildren<BallController>() : null;
            if (ball != null && !balls.Contains(ball)) balls.Add(ball);
        }
    }

    private void OnDestroy()
    {
        if (splineAnimate != null)
            splineAnimate.Completed -= OnSplineCompleted;
    }

    private void OnSplineCompleted()
    {
        if (State != BusState.FollowRoad || hasFinishedRoad) return;

        hasFinishedRoad = true;
        StartCoroutine(FinishRoad());
    }

    private System.Collections.IEnumerator FinishRoad()
    {
        ChangeState(BusState.Finished);

        // The ordered Ball Transforms list is authored in the Inspector.
        // Scale the balls one by one as soon as Car_Release begins.
        yield return ScaleBallsForReleaseStaggered(0.2f, 0.02f);

        DropBall();
        busAnimationController?.PlayMove();
        yield return ExitAndRecycle();
    }

    private void PrepareBallsForRelease()
    {
        foreach (BallController ball in balls)
        {
            if (ball != null)
                ball.transform.localScale = Vector3.one * 0.45f;
        }
    }

    private System.Collections.IEnumerator ScaleBallsForReleaseStaggered(float duration, float delayBetweenBalls)
    {
        foreach (Transform slot in ballTransforms)
        {
            if (slot == null) continue;
            BallController ball = slot.GetComponentInChildren<BallController>();
            if (ball == null) continue;

            StartCoroutine(ScaleBallForRelease(ball.transform, duration));
            yield return new WaitForSeconds(delayBetweenBalls);
        }

        // Let the last ball finish its own scale animation before releasing.
        yield return new WaitForSeconds(duration);
    }

    private System.Collections.IEnumerator ScaleBallForRelease(Transform ballTransform, float duration)
    {
        if (ballTransform == null) yield break;

        Vector3 startScale = ballTransform.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            ballTransform.localScale = Vector3.Lerp(startScale, Vector3.one * 0.45f, t);
            yield return null;
        }
    }

    private System.Collections.IEnumerator ExitAndRecycle()
    {
        if (isDespawning) yield break;
        isDespawning = true;
        ChangeState(BusState.Despawn);

        Vector3 exitDirection = Vector3.right;
        Vector3 exitTarget = transform.position + exitDirection * ExitDistance;
        float speed = ExitSpeed;

        while (speed > 0f && (transform.position - exitTarget).sqrMagnitude > 0.0001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, exitTarget, speed * Time.deltaTime);
            yield return null;
        }

        ObjectPool.Recycle(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider == null || collision.collider.transform.IsChildOf(transform)) return;
        HitDirection direction = BusUtils.GetHitDirection(transform, collision.collider);
        ChangeState(BusState.GotHit);
        busAnimationController?.PlayHit(direction);
        VfxManager.Play(VfxType.BusHit, transform.position);
        Hit?.Invoke(direction);
        SetBlocked(true);
    }

    public void StartMoveStraight()
    {
        ChangeState(BusState.MoveStraight);
        if (data != null && data.IsHurryBusMechanic())
            busAnimationController?.PlayHurry();
        if (movementRoutine != null) StopCoroutine(movementRoutine);
        movementRoutine = StartCoroutine(MoveToRoadPoint());
    }

    private void Start()
    {
        ApplyColor();
        busAnimationController?.PlayIdle();
    }

    private void Update()
    {
        if (State == BusState.FollowRoad && !hasOpenedOnRoad && splineAnimate != null &&
            splineAnimate.NormalizedTime >= 0.8f)
        {
            hasOpenedOnRoad = true;
            busAnimationController?.PlayOpen(data.busType);
        }

        if (!enableClickInput) return;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TrySelect(Mouse.current.position.ReadValue());
#else
        if (Input.GetMouseButtonDown(0))
            TrySelect(Input.mousePosition);
#endif
    }

    private void LateUpdate()
    {
        if (State != BusState.FollowRoad || splineAnimate == null || splineAnimate.Container == null)
            return;

        Spline spline = splineAnimate.Container.Spline;
        if (spline == null || spline.Count == 0) return;

        int segmentCount = spline.Closed ? spline.Count : spline.Count - 1;
        if (segmentCount <= 0) return;

        float scaledT = Mathf.Clamp01(splineAnimate.NormalizedTime) * segmentCount;
        int currentKnotIndex = Mathf.Min(Mathf.FloorToInt(scaledT), spline.Count - 1);
        if (currentKnotIndex != lastReachedKnotIndex)
        {
            float newKnotZRotation = (splineAnimate.Container.transform.rotation *
                ToUnityQuaternion(spline[currentKnotIndex].Rotation)).eulerAngles.z;
            if (!hasOpenedOnRoad)
                PlayTurnAnimation(activeKnotZRotation, newKnotZRotation);
            activeKnotZRotation = newKnotZRotation;
            lastReachedKnotIndex = currentKnotIndex;
        }

        transform.rotation = Quaternion.Euler(0f, 0f, activeKnotZRotation);
    }

    private static Quaternion ToUnityQuaternion(Unity.Mathematics.quaternion rotation)
    {
        return new Quaternion(rotation.value.x, rotation.value.y, rotation.value.z, rotation.value.w);
    }

    private void PlayTurnAnimation(float currentZRotation, float nextZRotation)
    {
        Vector3 currentDirection = Quaternion.Euler(0f, 0f, currentZRotation) * Vector3.right;
        Vector3 nextDirection = Quaternion.Euler(0f, 0f, nextZRotation) * Vector3.right;
        float directionDot = Vector3.Dot(currentDirection, nextDirection);
        float directionCrossZ = Vector3.Cross(currentDirection, nextDirection).z;

        if (directionDot > 0.999f)
            busAnimationController?.PlayMove();
        else if (directionCrossZ > 0f)
            busAnimationController?.PlayMoveLeft();
        else
            busAnimationController?.PlayMoveRight();
    }

    private void TrySelect(Vector2 screenPosition)
    {
        if (!CanInteract() || Camera.main == null) return;
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f)) return;
        if (hit.collider.transform != transform && !hit.collider.transform.IsChildOf(transform)) return;

        // Block relationship is calculated in the level JSON. The physics check
        // is intentionally not used to decide the gameplay state.
        if (analysisData != null && analysisData.IsBlocked())
        {
            SetBlocked(true);
            busAnimationController?.PlayHit(HitDirection.Front);
            float blockDistance = busManager != null ? busManager.EnterDistance : 0.5f;
            Bus blockingBus = GetNearestBlockingBus(transform.forward, blockDistance);
            blockingBus?.busAnimationController?.PlayStuck();
            if (blockingBus != null)
                VfxManager.Play(VfxType.StuckBus, blockingBus.transform.position);
            return;
        }

        SetBlocked(false);
        StartMoveStraight();
    }

    private bool CanInteract()
    {
        // Cho phép click lại khi bus đang bị chặn để phát lại Car_Hit.
        return State == BusState.Idle || State == BusState.Blocked;
    }

    private bool HasBlockingObject(Vector3 direction, float distance)
    {
        Vector3 origin = transform.position + direction.normalized * distance * 0.5f;
        Vector3 halfExtents = new(0.4f, 0.4f, Mathf.Max(0.1f, distance * 0.5f));
        Collider[] hits = Physics.OverlapBox(origin, halfExtents, transform.rotation, blockMask);
        foreach (Collider hit in hits)
            if (hit != null && hit.transform != transform && !hit.transform.IsChildOf(transform)) return true;
        return false;
    }

    private bool TryGetBlockingBus(Vector3 direction, float distance, out Bus blockingBus)
    {
        blockingBus = null;
        Vector3 origin = transform.position + direction.normalized * distance * 0.5f;
        Vector3 halfExtents = new(0.4f, 0.4f, Mathf.Max(0.1f, distance * 0.5f));
        Collider[] hits = Physics.OverlapBox(origin, halfExtents, transform.rotation, blockMask);

        foreach (Collider hit in hits)
        {
            if (hit == null || hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            blockingBus = hit.GetComponentInParent<Bus>();
            if (blockingBus != null && blockingBus != this)
                return true;
        }

        return false;
    }

    private System.Collections.IEnumerator MoveToRoadPoint()
    {
        Vector3 parkingPosition = transform.position;
        Vector3 moveDirection = transform.right;
        moveDirection.z = 0f;
        moveDirection.Normalize();
        float enterDistance = busManager != null ? busManager.EnterDistance : 0.05f;
        float speed = busManager != null ? busManager.MoveSpeed : 0f;

        if (speed <= 0f)
        {
            Debug.LogWarning($"Bus {name}: BusManager or MoveSpeed is not configured.");
            yield break;
        }

        float distanceToRoad = Mathf.Max(0f,
            Vector3.Dot(pathData.nearestPoint - parkingPosition, moveDirection));
        float movedDistance = 0f;

        while (movedDistance < distanceToRoad - enterDistance)
        {
            float step = Mathf.Min(speed * Time.deltaTime, distanceToRoad - movedDistance);
            transform.position += moveDirection * step;
            movedDistance += step;
            if (!releasedBlockedBuses &&
                (transform.position - parkingPosition).sqrMagnitude >= enterDistance * enterDistance)
                ReleaseBlockedBuses();
            yield return null;
        }

        // A very short path can reach the road before crossing EnterDistance.
        ReleaseBlockedBuses();

        ChangeState(BusState.EnterRoad);
        ChangeState(BusState.FollowRoad);
        if (splineAnimate == null)
        {
            Debug.LogWarning($"Bus {name}: SplineAnimate is not assigned.");
            yield break;
        }

        if (roadPath == null || !roadPath.TryGetRoad(pathData.splineContainerIndex, out SplineContainer road))
        {
            Debug.LogWarning($"Bus {name}: Road index {pathData.splineContainerIndex} is not configured.");
            yield break;
        }

        splineAnimate.Container = road;
        splineAnimate.Alignment = SplineAnimate.AlignmentMode.None;
        splineAnimate.NormalizedTime = pathData.nearestT;
        hasFinishedRoad = false;
        hasOpenedOnRoad = false;
        lastReachedKnotIndex = GetReachedKnotIndex(road.Spline, pathData.nearestT);
        activeKnotZRotation = transform.eulerAngles.z;
        ApplyKnotRotation(road.Spline, lastReachedKnotIndex);
        splineAnimate.Play();
    }

    private static int GetReachedKnotIndex(Spline spline, float normalizedTime)
    {
        int segmentCount = spline.Closed ? spline.Count : spline.Count - 1;
        if (segmentCount <= 0) return -1;
        float scaledT = Mathf.Clamp01(normalizedTime) * segmentCount;
        return Mathf.Min(Mathf.FloorToInt(scaledT), spline.Count - 1);
    }

    private void ApplyKnotRotation(Spline spline, int knotIndex)
    {
        if (knotIndex < 0 || knotIndex >= spline.Count || splineAnimate.Container == null) return;

        float knotZRotation = (splineAnimate.Container.transform.rotation *
            ToUnityQuaternion(spline[knotIndex].Rotation)).eulerAngles.z;
        PlayTurnAnimation(activeKnotZRotation, knotZRotation);
        activeKnotZRotation = knotZRotation;
        transform.rotation = Quaternion.Euler(0f, 0f, activeKnotZRotation);
    }

    public void Configure(BusData data, Vector3 position, float zRotation)
    {
        this.data = data?.Clone() ?? new BusData();
        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, zRotation));
        ApplyColor();
    }

    public void Configure(BusData data, BusAnalysisData analysisData, BusPathData pathData,
        Vector3 position, float zRotation)
    {
        this.analysisData = analysisData?.Clone() ?? new BusAnalysisData();
        this.pathData = pathData?.Clone() ?? new BusPathData();
        Configure(data, position, zRotation);
    }

    public void SetBallPrefab(BallController prefab)
    {
        ballPrefab = prefab;
        SpawnStaticBall();
    }

    public void SpawnStaticBall()
    {
        SpawnBalls();
    }

    private void SpawnBalls()
    {
        if (ballPrefab == null || balls.Count > 0 || data == null) return;

        int ballCount = Mathf.Min(data.GetBallCapacity(), ballTransforms.Count);
        for (int i = 0; i < ballCount; i++)
        {
            Transform slot = ballTransforms[i];
            if (slot == null) continue;

            BallController ball = ObjectPool.Spawn(ballPrefab, slot, slot.position, slot.rotation);
            if (ball == null) continue;

            ball.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            ball.Initialize(data.colorType, false);
            balls.Add(ball);
        }
    }

    public void ClearBlockingBuses()
    {
        blockingBuses.Clear();
    }

    public void SetLevelIndex(int index)
    {
        levelIndex = index;
    }

    private Bus GetNearestBlockingBus(Vector3 direction, float distance)
    {
        Vector3 origin = transform.position + direction.normalized * distance * 0.5f;
        Vector3 halfExtents = new(0.4f, 0.4f, Mathf.Max(0.1f, distance * 0.5f));
        Collider[] overlaps = Physics.OverlapBox(origin, halfExtents, transform.rotation, blockMask);

        Bus nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (Collider overlap in overlaps)
        {
            if (overlap == null) continue;

            Bus bus = overlap.GetComponentInParent<Bus>();
            if (bus == null || !blockingBuses.Contains(bus)) continue;

            float currentDistance = (bus.transform.position - transform.position).sqrMagnitude;
            if (currentDistance < nearestDistance ||
                (Mathf.Approximately(currentDistance, nearestDistance) &&
                 (nearest == null || bus.LevelIndex < nearest.LevelIndex)))
            {
                nearest = bus;
                nearestDistance = currentDistance;
            }
        }

        if (nearest != null) return nearest;

        // Fallback when the JSON relation exists but colliders do not overlap yet.
        foreach (Bus bus in blockingBuses)
        {
            if (bus == null) continue;
            if (nearest == null || bus.LevelIndex < nearest.LevelIndex)
                nearest = bus;
        }
        return nearest;
    }

    public void AddBlockingBus(Bus bus)
    {
        if (bus != null && bus != this && !blockingBuses.Contains(bus))
            blockingBuses.Add(bus);
    }

    public void AddBlockedBus(Bus bus)
    {
        if (bus != null && bus != this && !busesBlockedByThis.Contains(bus))
            busesBlockedByThis.Add(bus);
    }

    private void ReleaseBlockedBuses()
    {
        if (releasedBlockedBuses) return;
        releasedBlockedBuses = true;

        foreach (Bus blockedBus in busesBlockedByThis)
            blockedBus?.RemoveBlockingBus(this);
    }

    private void RemoveBlockingBus(Bus blockingBus)
    {
        if (!blockingBuses.Remove(blockingBus)) return;

        analysisData.blockedBusCount = Mathf.Max(0, analysisData.blockedBusCount - 1);
        if (analysisData.IsBlocked()) return;

        if (isBlocked)
        {
            isBlocked = false;
            BlockedChanged?.Invoke(false);
        }

        if (State == BusState.Blocked)
            ChangeState(BusState.Idle);
    }

    private void ApplyColor()
    {
        if (data == null)
        {
            Debug.Log($"Bus {name}: data is null in ApplyColor");
            return;
        }
        ColorType colorType = data.colorType;
        Debug.Log($"Bus {name}: ApplyColor called with colorType={colorType}");

        string matName = GetMaterialNameSuffix(colorType);
        if (!string.IsNullOrEmpty(matName))
        {
            // Apply bus body material
            if (bodyRenderer != null)
            {
                string path = $"Materials/Color/M_Color_{matName}";
                Material mat = Resources.Load<Material>(path);
                if (mat != null)
                {
                    bodyRenderer.sharedMaterial = mat;
                    Debug.Log($"Bus {name}: Loaded and applied body material {path}");
                }
                else
                {
                    Debug.LogWarning($"Bus {name}: Failed to load body material {path}!");
                }
            }

            // Apply glass material
            if (glassRenderer != null)
            {
                string path = $"Materials/Glass/M_Glass_{matName}";
                Material mat = Resources.Load<Material>(path);
                if (mat != null)
                {
                    Material[] materials = glassRenderer.sharedMaterials;
                    if (materials == null || materials.Length == 0)
                        glassRenderer.sharedMaterial = mat;
                    else
                    {
                        for (int i = 0; i < materials.Length; i++)
                            materials[i] = mat;
                        glassRenderer.sharedMaterials = materials;
                    }
                    Debug.Log($"Bus {name}: Loaded and applied glass material {path}");
                }
                else
                {
                    Debug.LogWarning($"Bus {name}: Failed to load glass material {path}!");
                }
            }
        }
        else
        {
            Debug.Log($"Bus {name}: matName suffix is null/empty for colorType={colorType}");
        }

        // Apply balls materials
        Debug.Log($"Bus {name}: Initializing {balls.Count} balls with colorType={colorType}");
        foreach (BallController ball in balls)
        {
            if (ball != null)
            {
                // Balls stored in the Bus must stay attached and non-physical
                // until Car_Release has completed.
                ball.Initialize(colorType, false);
            }
        }
    }

    private string GetMaterialNameSuffix(ColorType colorType)
    {
        return colorType switch
        {
            ColorType.Red => "Red",
            ColorType.Green => "Green",
            ColorType.Blue => "Blue",
            ColorType.Yellow => "Yellow",
            ColorType.Purple => "Purple",
            ColorType.Orange => "Orange",
            ColorType.Pink => "Pink",
            ColorType.Cyan => "BlueLight",
            ColorType.Forest => "GreenLight",
            ColorType.Brown => "Brown",
            ColorType.Black => "Black",
            _ => null
        };
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
        switch (nextState)
        {
            case BusState.Idle: busAnimationController?.PlayIdle(); break;
            case BusState.GarageOut: busAnimationController?.PlayGarageOut(); break;
            case BusState.MoveStraight:
            case BusState.EnterRoad:
            case BusState.FollowRoad: busAnimationController?.PlayMove(); break;
            case BusState.Blocked: busAnimationController?.PlayBlocked(); break;
            case BusState.Finished: busAnimationController?.PlayRelease(); break;
        }
        StateChanged?.Invoke(nextState);
    }

    public List<BallController> DropBall()
    {
        // Released balls belong to BallStart, which is the gameplay parent for
        // balls after they leave a bus. Their world-space formation is retained.
        Transform parent = topGameController != null && topGameController.BallStart != null
            ? topGameController.BallStart
            : dropParent != null ? dropParent : funnel?.BallHolder;
        return DropBallRow(parent);
    }

    public List<BallController> DropBall(Transform parent)
    {
        return DropBallRow(parent);
    }

    public List<BallController> DropBallRow(Transform parent)
    {
        List<BallController> dropped = new(balls);
        balls.Clear();
        for (int i = 0; i < dropped.Count; i++)
            ReleaseBall(dropped[i], parent, i, dropped.Count);
        return dropped;
    }

    public void DropBallRowIndividually(Transform parent)
    {
        StartCoroutine(DropBallRowIndividuallyCoroutine(parent));
    }

    private System.Collections.IEnumerator DropBallRowIndividuallyCoroutine(Transform parent)
    {
        List<BallController> dropped = new(balls);
        balls.Clear();
        for (int i = 0; i < dropped.Count; i++)
        {
            ReleaseBall(dropped[i], parent, i, dropped.Count);
            yield return new WaitForSeconds(0.08f);
        }
    }

    private void ReleaseBall(BallController ball, Transform parent, int index, int count)
    {
        if (ball == null) return;

        // Keep the exact formation authored under BallPosition. Re-parenting with
        // worldPositionStays preserves each ball's current world position.
        ball.transform.SetParent(parent, true);
        Vector3 releasePosition = ball.transform.position;
        releasePosition.z = -0.46f;
        ball.transform.position = releasePosition;
        ball.transform.localScale = Vector3.one * 0.45f;
        ball.DisablePhysics();
        ball.SetColliders(false);
        ball.Undock();
        VfxManager.Play(VfxType.BusReleaseBall, releasePosition);
        funnel?.ReceiveBall(ball);
    }

    public bool AddBall(BallController ball)
    {
        if (ball == null || balls.Contains(ball)) return false;
        balls.Add(ball);
        ball.transform.SetParent(transform, true);
        return true;
    }
}
